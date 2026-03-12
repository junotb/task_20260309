using FluentValidation.Results;
using task_20260309.Application.Common;
using task_20260309.Application.Employee.Commands;
using task_20260309.Domain.ValueObjects;

namespace task_20260309.Application.Employee;

/// <summary>
/// 직원 Import 공통 로직. 병합, 검증 매핑.
/// </summary>
internal static class EmployeeImportHelpers
{
    /// <summary>
    /// 여러 소스의 직원 목록을 이메일 기준으로 병합합니다.
    /// </summary>
    public static (List<EmployeeImportDto> Merged, List<ImportValidationError> DuplicateErrors) MergeByEmail(
        IEnumerable<EmployeeImportDto> sources)
    {
        var (merged, duplicates) = MergeByKeyHelper.MergeByKey(sources, e => Email.Normalize(e.Email));
        var duplicateErrors = duplicates
            .Select(d => new ImportValidationError(d.Index, d.Item.Email, ["같은 이메일이 이미 포함되어 있습니다."]))
            .ToList();
        return (merged, duplicateErrors);
    }

    /// <summary>
    /// FluentValidation ValidationResult를 ImportValidationError 목록으로 변환합니다.
    /// </summary>
    public static List<ImportValidationError> MapValidationResultToErrors(
        ValidationResult result,
        IReadOnlyList<EmployeeImportDto> employees)
    {
        if (result.IsValid) return [];

        var byIndex = new Dictionary<int, List<string>>();
        foreach (var failure in result.Errors)
        {
            var idx = FluentValidationExtensions.ExtractIndexFromPropertyName(failure.PropertyName);
            if (idx >= 0 && idx < employees.Count)
            {
                if (!byIndex.TryGetValue(idx, out var list))
                {
                    list = [];
                    byIndex[idx] = list;
                }
                list.Add(failure.ErrorMessage);
            }
        }

        return byIndex
            .OrderBy(kv => kv.Key)
            .Select(kv =>
            {
                var emp = employees[kv.Key];
                return new ImportValidationError(kv.Key + 1, emp.Email, kv.Value);
            })
            .ToList();
    }
}
