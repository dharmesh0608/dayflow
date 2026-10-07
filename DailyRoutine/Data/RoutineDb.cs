using DailyRoutine.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyRoutine.Data;

public class RoutineDb(DbContextOptions<RoutineDb> options) : DbContext(options)
{
    public DbSet<RoutineTask> Tasks => Set<RoutineTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var task = modelBuilder.Entity<RoutineTask>();

        task.HasIndex(t => t.Date);

        // Explicit conversions keep DateOnly/TimeOnly portable with the MySQL provider.
        task.Property(t => t.Date)
            .HasConversion(
                date => date.ToDateTime(TimeOnly.MinValue),
                value => DateOnly.FromDateTime(value))
            .HasColumnType("date");

        task.Property(t => t.StartTime)
            .HasConversion(
                time => time.ToTimeSpan(),
                value => TimeOnly.FromTimeSpan(value))
            .HasColumnType("time");

        task.Property(t => t.EndTime)
            .HasConversion(
                time => time.ToTimeSpan(),
                value => TimeOnly.FromTimeSpan(value))
            .HasColumnType("time");
    }
}
