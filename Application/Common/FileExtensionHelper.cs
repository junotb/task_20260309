namespace task_20260309.Application.Common;

/// <summary>
/// 파일 확장자 정규화. 파서 선택 등에서 사용.
/// </summary>
public static class FileExtensionHelper
{
    /// <summary>
    /// 파일명에서 확장자를 추출해 소문자로 반환. 없으면 null.
    /// 예: "data.csv" → "csv", "export.JSON" → "json"
    /// </summary>
    public static string? GetNormalizedExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return string.IsNullOrEmpty(ext) ? null : ext;
    }
}
