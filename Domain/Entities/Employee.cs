using task_20260309.Domain.ValueObjects;

namespace task_20260309.Domain.Entities;

/// <summary>
/// 직원 엔티티. DB 저장 시 이메일은 Email VO로 정규화됨.
/// </summary>
public class Employee
{
    /// <summary>DB PK. 자동 생성. 예: 1</summary>
    public int Id { get; set; }
    /// <summary>이름. 필수, 최대 200자. 예: 홍길동</summary>
    public required string Name { get; set; }
    /// <summary>이메일. 필수, 유일. Email VO로 저장. 예: hong@example.com</summary>
    public required Email Email { get; set; }
    /// <summary>전화번호. 필수, 숫자/하이픈/공백. 예: 010-1234-5678</summary>
    public required string Tel { get; set; }
    /// <summary>입사일. 필수. 예: 2024-01-15</summary>
    public DateTime Joined { get; set; }
}
