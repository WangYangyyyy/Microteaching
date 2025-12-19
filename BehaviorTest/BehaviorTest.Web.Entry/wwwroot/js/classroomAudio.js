// wwwroot/js/classroomAudio.js
// Furion/Blazor 使用：export async function startClassroomLoop(...) / export function stopClassroomLoop()

let _stream = null;
let _recorder = null;
let _chunks = [];
let _recording = false;

// ===== 静音检测 / 音量检测 =====
let _audioCtx = null;
let _analyser = null;
let _sourceNode = null;

let _volumeTimer = null;
let _silenceTimer = null;

let _silenceThresholdMs = 1800;   // 切段：连续静音 >= 该值就 stop
let _volumeThreshold = 2;        // 音量大于这个值就认为“在说话”
let _useAdaptiveThreshold = true; // 是否根据底噪自适应阈值
let _noiseSampleMs = 1000;        // 自适应：采样底噪时长
let _noiseMargin = 2;            // 自适应：阈值 = 底噪 + 裕度
let _minRecordMs = 1200;          // 最短录音时长：避免刚开口就被截断
let _maxSilentFrames = 10;        // 连续静音帧数（100ms*帧数 ~= 静音时长）
let _smoothWindow = 5;            // 平滑窗口

let _volumeHistory = [];
let _silenceFrameCount = 0;
let _recordStartTime = 0;

function _stripWakeWord(raw, wakeWord) {
    if (!raw) return null;
    const t = String(raw).trim();
    if (!wakeWord) return t;

    if (t.startsWith(wakeWord + "：") || t.startsWith(wakeWord + ":")) {
        return t.slice(wakeWord.length + 1).trim();
    }
    if (t.startsWith(wakeWord)) {
        return t.slice(wakeWord.length).trim();
    }
    if (t.includes(wakeWord)) {
        return t.replace(wakeWord, "").replace("：", "").replace(":", "").trim();
    }
    return null;
}

function _stopTimers() {
    if (_volumeTimer) {
        clearInterval(_volumeTimer);
        _volumeTimer = null;
    }
    if (_silenceTimer) {
        clearInterval(_silenceTimer);
        _silenceTimer = null;
    }
}

function _resetVolumeState() {
    _volumeHistory = [];
    _silenceFrameCount = 0;
}

async function _ensureAnalyser() {
    if (_audioCtx && _analyser && _sourceNode) return;

    _audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    _analyser = _audioCtx.createAnalyser();
    _analyser.fftSize = 2048;

    if (!_stream) {
        _stream = await navigator.mediaDevices.getUserMedia({audio: true});
    }
    _sourceNode = _audioCtx.createMediaStreamSource(_stream);
    _sourceNode.connect(_analyser);
}

async function _ensureAudioRunning() {
    if (_audioCtx && _audioCtx.state === "suspended") {
        try {
            await _audioCtx.resume();
        } catch {
        }
    }
}

// 计算时间域 RMS（0-100 之间的相对值）
function _getRmsVolume() {
    try {
        // ✅ 兜底：analyser 或 ctx 状态不对，就返回 0
        if (!_analyser || !_audioCtx || _audioCtx.state !== "running") return 0;

        const bufferLength = _analyser.fftSize;
        if (!bufferLength || bufferLength <= 0) return 0;

        const dataArray = new Float32Array(bufferLength);
        _analyser.getFloatTimeDomainData(dataArray);

        let sum = 0;
        for (let i = 0; i < bufferLength; i++) {
            const v = dataArray[i];
            sum += v * v;
        }
        const rms = Math.sqrt(sum / bufferLength); // 0-1
        return Math.min(1, rms) * 100;
    } catch {
        // ✅ 任何异常都不允许冒泡到 interval
        return 0;
    }
}

// 平滑 RMS，减少抖动
function _getSmoothedVolume() {
    const v = _getRmsVolume();
    _volumeHistory.push(v);
    if (_volumeHistory.length > _smoothWindow) _volumeHistory.shift();
    const total = _volumeHistory.reduce((a, b) => a + b, 0);
    return total / _volumeHistory.length;
}

// 采样底噪并返回建议阈值
async function _measureNoiseFloor(durationMs = 1000, intervalMs = 100) {
    _resetVolumeState();
    const samples = [];
    const start = Date.now();

    return await new Promise(resolve => {
        const timer = setInterval(() => {
            samples.push(_getSmoothedVolume());
            if (Date.now() - start >= durationMs) {
                clearInterval(timer);
                if (samples.length === 0) {
                    resolve(0);
                    return;
                }
                const avg = samples.reduce((a, b) => a + b, 0) / samples.length;
                resolve(avg);
            }
        }, intervalMs);
    });
}

async function _postRecognize(apiBase, blob) {
    const fd = new FormData();
    fd.append("audio", blob, "recording.webm");

    const r = await fetch(`${apiBase}/recognize`, {method: "POST", body: fd});
    if (!r.ok) {
        const txt = await r.text().catch(() => "");
        throw new Error(`/recognize failed: ${r.status} ${txt}`);
    }

    const data = await r.json();

    // 兼容两种后端结构：
    // A) 旧：{ text: "{\"text\":\"...\"}" } 需要 JSON.parse
    // B) 新：{ text: "..." } 直接用
    let text = "";
    try {
        if (typeof data.text === "string") {
            const maybeObj = JSON.parse(data.text);
            text = (maybeObj && maybeObj.text) ? String(maybeObj.text) : String(data.text);
        } else if (data && typeof data.text !== "undefined") {
            text = String(data.text);
        }
    } catch {
        text = String(data.text || "");
    }

    return text.trim();
}

async function _postHuman(apiBase, question, sessionid) {
    const r = await fetch(`${apiBase}/human`, {
        method: "POST",
        headers: {"Content-Type": "application/json"},
        body: JSON.stringify({text: question, sessionid})
    });
    if (!r.ok) {
        const txt = await r.text().catch(() => "");
        throw new Error(`/human failed: ${r.status} ${txt}`);
    }
    return await r.json();
}

// 开始一轮录音：直到“静音达到阈值”才 stop → 触发 onstop → 发送识别
async function _recordOnceAndProcess(dotNetRef, apiBase, wakeWord, silenceMs, sessionid) {
    if (_recording) return;

    if (!_stream) {
        _stream = await navigator.mediaDevices.getUserMedia({audio: true});
    }

    _silenceThresholdMs = silenceMs || 2000;

    // ✅ 只允许一个 analyser/ctx
    await _ensureAnalyser();
    await _ensureAudioRunning();

    // ✅ 自适应阈值（可选）
    if (_useAdaptiveThreshold) {
        const noise = await _measureNoiseFloor(_noiseSampleMs, 100);
        _volumeThreshold = Math.max(_volumeThreshold, Math.round(noise + _noiseMargin));
    }

    _chunks = [];
    _resetVolumeState();
    _silenceFrameCount = 0;
    _recordStartTime = Date.now();

    _recorder = new MediaRecorder(_stream, {mimeType: "audio/webm"});

    _recorder.ondataavailable = (e) => {
        if (e.data && e.data.size > 0) _chunks.push(e.data);
    };

    _recorder.onstop = async () => {
        // ✅ stop 一进来先停 timer，避免 stop 后 interval 再 tick 一次
        _stopTimers();

        try {
            await dotNetRef.invokeMethodAsync("SetStatus", "识别中 /recognize ...");

            const blob = new Blob(_chunks, {type: "audio/webm"});
            const raw = await _postRecognize(apiBase, blob);

            await dotNetRef.invokeMethodAsync("OnAsrRaw", raw || "");

            const question = _stripWakeWord(raw, wakeWord);
            if (!question) {
                await dotNetRef.invokeMethodAsync("SetStatus", "未检测到唤醒词，继续监听...");
                return;
            }
            if (!question.trim()) {
                await dotNetRef.invokeMethodAsync("SetStatus", "唤醒词后无问题内容，继续监听...");
                return;
            }

            await dotNetRef.invokeMethodAsync("OnTeacherQuestion", question);

            await dotNetRef.invokeMethodAsync("SetStatus", "请求三位学生回答 /human ...");
            const human = await _postHuman(apiBase, question, sessionid);

            const answers = Array.isArray(human.answers) ? human.answers : [];
            await dotNetRef.invokeMethodAsync("OnStudentAnswers", answers);

            await dotNetRef.invokeMethodAsync("SetStatus", "完成一轮，继续监听...");
        } catch (err) {
            await dotNetRef.invokeMethodAsync("OnError", err && err.message ? err.message : String(err));
            // 出错也继续下一轮
            try {
                await dotNetRef.invokeMethodAsync("SetStatus", "发生错误，继续监听...");
            } catch {
            }
        } finally {
            _recording = false;
            _recordLoop(dotNetRef, apiBase, wakeWord, _silenceThresholdMs, sessionid);
        }
    };

    _recorder.start();
    _recording = true;

    await dotNetRef.invokeMethodAsync("SetStatus", "录音中（静音阈值切段）...");

    // ====== 静音检测逻辑（双 timer：音量采样 + 静音判断）======
    _stopTimers();

    // 每 100ms 采一次音量，更新静音计数
    _volumeTimer = setInterval(() => {
        try {
            if (!_recording || !_recorder || _recorder.state !== "recording") return;

            // ✅ ctx/analyser 可能被浏览器挂起/清空
            const v = _getSmoothedVolume();
            
            // ======调试vad=====
            _vadDebugRender({
                recState: _recorder ? _recorder.state : "null",
                ctxState: _audioCtx ? _audioCtx.state : "null",
                v,
                thr: _volumeThreshold,
                silentFrames: _silenceFrameCount,
                silentMs: _silenceThresholdMs,
                minRecMs: _minRecordMs,
                recMs: Date.now() - _recordStartTime,
                useAdaptive: _useAdaptiveThreshold,
                noiseSampleMs: _noiseSampleMs,
                noiseMargin: _noiseMargin
            });


            if (v > _volumeThreshold) {
                _silenceFrameCount = 0;
            } else {
                _silenceFrameCount++;
            }
        } catch {
            // 不允许冒泡
        }
    }, 100);

    // 每 100ms 判断是否该切段
    _silenceTimer = setInterval(() => {
        try {
            if (!_recording || !_recorder || _recorder.state !== "recording") return;

            const elapsed = Date.now() - _recordStartTime;

            // ✅ 录得太短不允许切段（避免刚开口被截断）
            if (elapsed < _minRecordMs) return;

            // ✅ 连续静音帧达到阈值（默认 10 帧 = 1 秒），同时也满足 silenceMs（更严格以 frames 为主）
            const silentMs = _silenceFrameCount * 100;

            if (silentMs >= _silenceThresholdMs) {
                _stopTimers();
                try {
                    _recorder.stop();
                } catch {
                }
            }
        } catch {
            // 不允许冒泡
        }
    }, 100);
}

function _recordLoop(dotNetRef, apiBase, wakeWord, silenceMs, sessionid) {
    _recordOnceAndProcess(dotNetRef, apiBase, wakeWord, silenceMs, sessionid);
}

// ====== 暴露给 Blazor 的 API（Furion 用）======

export async function startClassroomLoop(dotNetRef, apiBase, wakeWord, silenceMs, sessionid,
                                         // 可选：从 C# 配置传进来覆盖默认值
                                         options = null
) {
    try {
        if (options) {
            if (typeof options.SilenceMs === "number") _silenceThresholdMs = options.SilenceMs;
            if (typeof options.VolumeThreshold === "number") _volumeThreshold = options.VolumeThreshold;
            if (typeof options.UseAdaptiveThreshold === "boolean") _useAdaptiveThreshold = options.UseAdaptiveThreshold;
            if (typeof options.NoiseSampleMs === "number") _noiseSampleMs = options.NoiseSampleMs;
            if (typeof options.NoiseMargin === "number") _noiseMargin = options.NoiseMargin;
            if (typeof options.MinRecordMs === "number") _minRecordMs = options.MinRecordMs;
            if (typeof options.SilentFrames === "number") _maxSilentFrames = options.SilentFrames;
            if (typeof options.SmoothWindow === "number") _smoothWindow = options.SmoothWindow;
        }

        await dotNetRef.invokeMethodAsync("SetStatus", "请求麦克风权限...");
        if (!_stream) _stream = await navigator.mediaDevices.getUserMedia({audio: true});

        await _ensureAnalyser();
        await _ensureAudioRunning();

        await dotNetRef.invokeMethodAsync("SetStatus", "开始监听...");
        _recordLoop(dotNetRef, apiBase, wakeWord, silenceMs, sessionid);
    } catch (err) {
        await dotNetRef.invokeMethodAsync("OnError", err && err.message ? err.message : String(err));
    }
}

export function stopClassroomLoop() {
    try {
        _stopTimers();

        if (_recorder && _recorder.state === "recording") {
            try {
                _recorder.stop();
            } catch {
            }
        }
    } catch {
    }

    _recording = false;

    // 关闭麦克风
    if (_stream) {
        try {
            _stream.getTracks().forEach(t => t.stop());
        } catch {
        }
        _stream = null;
    }

    // 关闭音频上下文
    if (_audioCtx) {
        try {
            _audioCtx.close();
        } catch {
        }
        _audioCtx = null;
        _analyser = null;
        _sourceNode = null;
    }
}

// =================调试用=================
let _debugVad = true;          // ✅ 调试开关：true 显示面板 + 打日志
let _debugEveryMs = 300;       // ✅ 每隔多少 ms 输出一次（别太频繁）
let _lastDebugTs = 0;

function _ensureVadPanel() {
    if (!_debugVad) return;

    if (document.getElementById("__vad_panel")) return;

    const div = document.createElement("div");
    div.id = "__vad_panel";
    div.style.cssText =
        "position:fixed;right:12px;bottom:12px;z-index:99999;" +
        "width:360px;max-width:92vw;padding:10px 12px;" +
        "background:rgba(0,0,0,0.75);color:#fff;" +
        "font:12px/1.4 monospace;border-radius:10px;" +
        "box-shadow:0 8px 20px rgba(0,0,0,.25)";
    div.innerHTML = `
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:6px;">
      <b>VAD Debug</b>
      <button id="__vad_btn" style="cursor:pointer;border:none;border-radius:8px;padding:2px 8px;">Hide</button>
    </div>
    <div id="__vad_body"></div>
  `;
    document.body.appendChild(div);

    const btn = document.getElementById("__vad_btn");
    const body = document.getElementById("__vad_body");
    btn.onclick = () => {
        if (body.style.display === "none") {
            body.style.display = "block";
            btn.textContent = "Hide";
        } else {
            body.style.display = "none";
            btn.textContent = "Show";
        }
    };
}

function _vadDebugRender(state) {
    if (!_debugVad) return;

    const now = Date.now();
    if (now - _lastDebugTs < _debugEveryMs) return;
    _lastDebugTs = now;

    _ensureVadPanel();
    const body = document.getElementById("__vad_body");
    if (!body) return;

    const {
        recState, ctxState, v, thr, silentFrames, silentMs, minRecMs, recMs,
        useAdaptive, noiseSampleMs, noiseMargin
    } = state;

    body.innerHTML = `
    recState: <b>${recState}</b><br/>
    ctxState: <b>${ctxState}</b><br/>
    volume(v): <b>${v.toFixed(1)}</b> / thr: <b>${thr}</b><br/>
    silentFrames: <b>${silentFrames}</b> (≈ ${(silentFrames * 100)}ms)<br/>
    silenceMs(threshold): <b>${silentMs}</b> | minRecMs: <b>${minRecMs}</b><br/>
    recordMs: <b>${recMs}</b><br/>
    adaptive: <b>${useAdaptive}</b> | sample: <b>${noiseSampleMs}</b> | margin: <b>${noiseMargin}</b>
  `;

    // 同时可选输出 console（方便回溯）
    console.log(
        `[VAD] v=${v.toFixed(1)} thr=${thr} silentFrames=${silentFrames} recMs=${recMs} ctx=${ctxState} rec=${recState}`
    );
}


// ====== 音频播放队列管理 ======
let _audioQueue = [];
let _isPlaying = false;

async function _playNext() {
    if (_audioQueue.length === 0) {
        _isPlaying = false;
        return;
    }

    _isPlaying = true;
    const base64Str = _audioQueue.shift();
    const audio = new Audio("data:audio/mp3;base64," + base64Str);

    // 播放结束自动播下一个
    audio.onended = () => {
        // 短暂停顿更自然
        setTimeout(() => {
            _playNext();
        }, 500);
    };

    audio.onerror = () => {
        console.error("Audio playback failed");
        _playNext();
    };

    try {
        await audio.play();
    } catch (e) {
        console.error("Play error:", e);
        _playNext();
    }
}

// 暴露给 C# 调用的方法
export function playStudentAudios(audioList) {
    if (!audioList || audioList.length === 0) return;

    // 将新音频加入队列
    for (let b64 of audioList) {
        if(b64) _audioQueue.push(b64);
    }

    // 如果当前没在播，就开始播
    if (!_isPlaying) {
        _playNext();
    }
}

