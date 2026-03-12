using task_20260309.Domain.Repositories;

namespace task_20260309.Application.Employee.Queries;

/// <summary>
/// 이름으로 직원 1건 조회. 정확 일치만. 부분 일치/검색 없음.
/// 실패 시: Repository 예외 전파. 미존재는 null 반환(예외 아님).
/// </summary>
public class GetEmployeeByNameQueryHandler
{
    private readonly IEmployeeRepository _repository;
    private readonly ILogger<GetEmployeeByNameQueryHandler> _logger;

    public GetEmployeeByNameQueryHandler(IEmployeeRepository repository, ILogger<GetEmployeeByNameQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<GetEmployeeByNameResponse> HandleAsync(GetEmployeeByNameQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Query 시작, QueryName={QueryName}, Name={Name}",
            nameof(GetEmployeeByNameQuery), query.Name);

        var employees = await _repository.GetAllByNameAsync(query.Name, ct);
        var dtos = employees
            .Select(e => new EmployeeDetailDto(e.Id, e.Name, e.Email.Value, e.Tel, e.Joined))
            .ToList();

        _logger.LogInformation(
            "Query 완료, QueryName={QueryName}, Name={Name}, Count={Count}",
            nameof(GetEmployeeByNameQuery), query.Name, dtos.Count);
        return new GetEmployeeByNameResponse(dtos);
    }
}
