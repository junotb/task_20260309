using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using task_20260309.Domain.Entities;
using task_20260309.Domain.ValueObjects;

namespace task_20260309.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();

    private static readonly ValueConverter<Email, string> EmailConverter =
        new(
            e => e.Normalized,
            s => Email.FromNormalized(s));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email)
                .HasConversion(EmailConverter)
                .IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Tel).IsRequired();
        });
    }
}
