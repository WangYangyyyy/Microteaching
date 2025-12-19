using System.Text.Json;
using System.Linq;
using Furion.LinqBuilder;
using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services;

public class QuestionAnswerService : IQuestionAnswerService, IDynamicApiController, ITransient
{
    private readonly IRepository<QaRecord> _qaRepository;

    public QuestionAnswerService(IRepository<QaRecord> qaRepository)
    {
        _qaRepository = qaRepository;
    }

    public async Task<OperationResult> SaveAsync(QaSaveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return OperationResult.FailureResult("问题内容不能为空。");
        }

        var answersJson = JsonSerializer.Serialize(request.Answers ?? new List<StudentAnswerDto>(),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var entity = new QaRecord
        {
            SessionId = request.SessionId,
            Question = request.Question.Trim(),
            AnswersJson = answersJson,
            AskedAt = request.AskedAt == default ? DateTime.UtcNow : request.AskedAt,
            CreatedAt = DateTime.UtcNow
        };

        await _qaRepository.InsertNowAsync(entity);
        return OperationResult.SuccessResult("问答已保存。", entity.Id);
    }

    public async Task<List<QaRecord>> GetRecentAsync(int take = 20)
    {
        take = Math.Clamp(take, 1, 100);
        return await _qaRepository.AsQueryable()
            .OrderByDescending(q => q.CreatedAt)
            .Take(take)
            .ToListAsync();
    }
}
