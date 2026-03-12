using System.Text.RegularExpressions;
using task_20260309.Domain.ValueObjects;

namespace task_20260309.Application.Common;

/// <summary>
/// 공통 검증 패턴. 이메일, 전화번호 등. 여러 Validator에서 재사용.
/// </summary>
public static class ValidationPatterns
{
    private static readonly Regex TelRegex = new(
        @"^[\d\-\s+()\.]+$",
        RegexOptions.Compiled);

    /// <summary>
    /// 이메일 형식 검사. Domain.Email.IsValidFormat 위임.
    /// </summary>
    public static bool IsValidEmail(string? email) => Email.IsValidFormat(email);

    /// <summary>
    /// 전화번호 형식 검사. 숫자/하이픈/공백/괄호/점 허용.
    /// </summary>
    public static bool IsValidTel(string? tel)
    {
        if (string.IsNullOrWhiteSpace(tel)) return false;
        return TelRegex.IsMatch(tel.Trim());
    }
}
