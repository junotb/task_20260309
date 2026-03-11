namespace task_20260309.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Tel { get; set; }
    public DateTime Joined { get; set; }
}
