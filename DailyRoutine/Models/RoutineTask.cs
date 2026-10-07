using System.ComponentModel.DataAnnotations;

namespace DailyRoutine.Models;

public class RoutineTask : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public TimeOnly StartTime { get; set; } = new(6, 0);

    public TimeOnly EndTime { get; set; } = new(7, 0);

    [Required]
    [RegularExpression("^(Study|College|Exercise|Meals|Personal|Work)$")]
    public string Category { get; set; } = "Study";

    [Required]
    [RegularExpression("^(Low|Medium|High)$")]
    public string Priority { get; set; } = "Medium";

    public bool IsCompleted { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult("Enter a task title.", new[] { nameof(Title) });
        }

        if (EndTime <= StartTime)
        {
            yield return new ValidationResult(
                "End time must be after start time. Split overnight tasks into two days.",
                new[] { nameof(EndTime) });
        }

        if (Date == default)
        {
            yield return new ValidationResult("Choose a valid date.", new[] { nameof(Date) });
        }
    }
}

public class Dashboard
{
    public DateOnly Date { get; set; }
    public List<RoutineTask> Tasks { get; set; } = new();
    public Dictionary<DateOnly, int> MonthCounts { get; set; } = new();
    public string? Search { get; set; }
    public string Status { get; set; } = "all";
    public DateOnly Today { get; set; }
    public TimeOnly Now { get; set; }
    public List<RoutineTask> WeekTasks { get; set; } = new();
}
