namespace task_20260309.Application.Common;

/// <summary>
/// FluentValidation RuleForEach 인덱스 추출 등. Import 유효성 매핑에 사용.
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// "Items[0].Email" 형태에서 인덱스 추출. 없으면 -1.
    /// </summary>
    public static int ExtractIndexFromPropertyName(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return -1;
        var start = propertyName.IndexOf('[');
        var end = propertyName.IndexOf(']', start + 1);
        if (start < 0 || end <= start) return -1;
        return int.TryParse(propertyName.AsSpan(start + 1, end - start - 1), out var idx) ? idx : -1;
    }
}
