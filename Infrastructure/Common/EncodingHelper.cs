using System.Text;

namespace task_20260309.Infrastructure.Common;

/// <summary>
/// 스트림 인코딩 감지 및 텍스트 변환. Import 파서 등에서 사용.
/// </summary>
public static class EncodingHelper
{
    private static Encoding? _eucKr;

    /// <summary>
    /// EUC-KR(CP949) 인코딩. CodePagesEncodingProvider 등록 후 반환.
    /// </summary>
    public static Encoding GetEucKr()
    {
        if (_eucKr is not null) return _eucKr;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _eucKr = Encoding.GetEncoding(949);
        return _eucKr;
    }

    /// <summary>
    /// UTF-8 시도 후 한글 깨짐(�) 있으면 EUC-KR 재시도.
    /// </summary>
    public static async Task<string> ReadStreamWithEncodingAsync(Stream stream, CancellationToken ct = default)
    {
        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        if (bytes.Length == 0) return string.Empty;

        var utf8 = Encoding.UTF8.GetString(bytes);
        if (!utf8.Contains('\uFFFD'))
            return utf8;

        try
        {
            return GetEucKr().GetString(bytes);
        }
        catch
        {
            return utf8;
        }
    }
}
