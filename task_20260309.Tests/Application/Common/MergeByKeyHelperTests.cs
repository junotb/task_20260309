using FluentAssertions;
using task_20260309.Application.Common;
using task_20260309.Application.Employee.Commands;
using Xunit;

namespace task_20260309.Tests.Application.Common;

/// <summary>
/// MergeByKeyHelper 단위 테스트.
/// 준호님이 만든 공통 헬퍼가 중복 이메일 등을 키값 기반으로 정확히 병합하는지 검증.
/// </summary>
public class MergeByKeyHelperTests
{
    [Fact]
    public void MergeByKey_중복_없으면_전부_Merged에()
    {
        var items = new List<EmployeeImportDto>
        {
            new("홍길동", "hong@example.com", "010-1111", DateTime.UtcNow.Date),
            new("김철수", "kim@example.com", "010-2222", DateTime.UtcNow.Date)
        };

        var (merged, duplicates) = MergeByKeyHelper.MergeByKey(items, e => e.Email);

        merged.Should().HaveCount(2);
        duplicates.Should().BeEmpty();
        merged[0].Email.Should().Be("hong@example.com");
        merged[1].Email.Should().Be("kim@example.com");
    }

    [Fact]
    public void MergeByKey_중복_이메일_시_먼저_나온_것_우선_Duplicates에_나머지()
    {
        var items = new List<EmployeeImportDto>
        {
            new("홍길동", "same@example.com", "010-1111", DateTime.UtcNow.Date),
            new("김철수", "same@example.com", "010-2222", DateTime.UtcNow.Date),
            new("이영희", "other@example.com", "010-3333", DateTime.UtcNow.Date)
        };

        var (merged, duplicates) = MergeByKeyHelper.MergeByKey(items, e => e.Email);

        merged.Should().HaveCount(2);
        merged[0].Email.Should().Be("same@example.com");
        merged[0].Name.Should().Be("홍길동");
        merged[1].Email.Should().Be("other@example.com");

        duplicates.Should().HaveCount(1);
        duplicates[0].Item.Email.Should().Be("same@example.com");
        duplicates[0].Item.Name.Should().Be("김철수");
        duplicates[0].Index.Should().Be(2); // 1-based
    }

    [Fact]
    public void MergeByKey_대소문자_무시_병합()
    {
        var items = new List<EmployeeImportDto>
        {
            new("First", "User@Example.COM", "010-1111", DateTime.UtcNow.Date),
            new("Second", "user@example.com", "010-2222", DateTime.UtcNow.Date)
        };

        var (merged, duplicates) = MergeByKeyHelper.MergeByKey(items, e => e.Email);

        merged.Should().HaveCount(1);
        merged[0].Name.Should().Be("First");
        duplicates.Should().HaveCount(1);
        duplicates[0].Index.Should().Be(2);
    }

    [Fact]
    public void MergeByKey_커스텀_Comparer_사용_가능()
    {
        var items = new List<string> { "a", "A", "b" };
        var (merged, duplicates) = MergeByKeyHelper.MergeByKey(items, x => x, StringComparer.Ordinal);

        merged.Should().HaveCount(3);
        duplicates.Should().BeEmpty();
    }

    [Fact]
    public void MergeByKey_빈_입력이면_빈_결과()
    {
        var (merged, duplicates) = MergeByKeyHelper.MergeByKey(
            Array.Empty<EmployeeImportDto>(),
            e => e.Email);

        merged.Should().BeEmpty();
        duplicates.Should().BeEmpty();
    }
}
