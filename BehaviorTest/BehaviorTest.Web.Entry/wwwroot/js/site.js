// wwwroot/js/site.js
(function () {
    function sanitizeFileName(name) {
        return (name || 'download.bin').replace(/[\\/:*?"<>|]/g, '_').trim();
    }

    function fileNameFromUrl(url) {
        try {
            const u = new URL(url, location.href);
            const segs = u.pathname.split('/').filter(Boolean);
            return segs.length ? segs[segs.length - 1] : 'download.bin';
        } catch {
            const segs = (url || '').split('/').filter(Boolean);
            return segs.length ? segs[segs.length - 1] : 'download.bin';
        }
    }

    function clickAnchor(href, fileName) {
        const a = document.createElement('a');
        a.href = href;
        a.rel = 'noopener';
        if ('download' in HTMLAnchorElement.prototype) {
            a.download = sanitizeFileName(fileName);
        } else {
            // 旧浏览器降级为新窗口打开
            window.open(href, '_blank');
            return;
        }
        a.style.display = 'none';
        document.body.appendChild(a);
        a.click();
        a.remove();
    }

    window.forceDownload = function (url, fileName) {
        try {
            if (!url) {
                alert('下载失败：无效的地址');
                return;
            }
            const name = fileName || fileNameFromUrl(url);
            clickAnchor(url, name);
        } catch (e) {
            console.error(e);
            alert('下载失败：' + e);
        }
    };

    function base64ToBytes(b64) {
        // 兼容 data:*/*;base64,xxxx 与纯 base64 两种输入
        const pure = (b64 || '').includes(',') ? b64.split(',').pop() : (b64 || '');
        const bin = atob(pure);
        const len = bin.length;
        const bytes = new Uint8Array(len);
        for (let i = 0; i < len; i++) bytes[i] = bin.charCodeAt(i);
        return bytes;
    }

    function mimeFromDataUrl(b64) {
        try {
            if ((b64 || '').startsWith('data:')) {
                const semi = b64.indexOf(';');
                return semi > 5 ? b64.substring(5, semi) : 'application/octet-stream';
            }
        } catch {}
        return 'application/octet-stream';
    }

    window.downloadBase64 = function (base64, fileName, mime) {
        try {
            if (!base64) {
                alert('下载失败：内容为空');
                return;
            }
            const bytes = base64ToBytes(base64);
            const type = mime || mimeFromDataUrl(base64);
            const blob = new Blob([bytes], { type });
            const url = URL.createObjectURL(blob);
            const name = sanitizeFileName(fileName || 'download.bin');
            clickAnchor(url, name);
            setTimeout(() => URL.revokeObjectURL(url), 30_000);
        } catch (e) {
            console.error(e);
            alert('下载失败：' + e);
        }
    };
})();
