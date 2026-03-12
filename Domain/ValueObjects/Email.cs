using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace task_20260309.Domain.ValueObjects;

/// <summary>
/// 이메일 Value Object. 정규화(Normalized)와 형식 검증 제공.
/// </summary>
public sealed record Email
{
    private static readonly Regex FormatRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 원본 값(Trim 적용). DB 저장·비교 시에는 Normalized 사용.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// DB 비교/저장용. Trim + ToLowerInvariant.
    /// </summary>
    public string Normalized { get; }

    private Email(string value, string normalized)
    {
        Value = value;
        Normalized = normalized;
    }

    /// <summary>
    /// 형식 검증. RFC 완전 일치 아님.
    /// </summary>
    public static bool IsValidFormat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return FormatRegex.IsMatch(value.Trim());
    }

    /// <summary>
    /// Trim + ToLowerInvariant. 검증 없음. 병합/비교용 키로 사용.
    /// </summary>
    public static string Normalize(string? value)
    {
        return (value?.Trim() ?? "").ToLowerInvariant();
    }

    /// <summary>
    /// 검증 후 Email 생성. 실패 시 예외.
    /// </summary>
    public static Email Create(string value)
    {
        var trimmed = value?.Trim() ?? "";
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("이메일은 필수입니다.", nameof(value));
        if (!IsValidFormat(trimmed))
            throw new ArgumentException("올바른 이메일 형식이 아닙니다.", nameof(value));
        if (trimmed.Length > 320)
            throw new ArgumentException("이메일은 320자를 초과할 수 없습니다.", nameof(value));
        return new Email(trimmed, trimmed.ToLowerInvariant());
    }

    /// <summary>
    /// 형식 검증 성공 시 Email 반환.
    /// </summary>
    public static bool TryCreate(string? value, [NotNullWhen(true)] out Email? email)
    {
        email = null;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value.Trim();
        if (!IsValidFormat(trimmed) || trimmed.Length > 320) return false;
        email = new Email(trimmed, trimmed.ToLowerInvariant());
        return true;
    }

    /// <summary>
    /// DB/저장소에서 읽을 때 사용. 이미 정규화된 값. 검증 스킵.
    /// </summary>
    public static Email FromNormalized(string normalized)
    {
        var s = (normalized ?? "").Trim().ToLowerInvariant();
        return new Email(s, s);
    }
}
