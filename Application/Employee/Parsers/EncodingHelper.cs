using System.Text;

namespace task_20260309.Application.Employee.Parsers;

internal static class EncodingHelper
{
    private static Encoding? _eucKr;

    public static Encoding GetEucKr()
    {
        if (_eucKr is not null) return _eucKr;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _eucKr = Encoding.GetEncoding(949);
        return _eucKr;
    }
}
