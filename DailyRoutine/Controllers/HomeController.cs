using System.Globalization;
using ClosedXML.Excel;
using DailyRoutine.Data;
using DailyRoutine.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRoutine.Controllers;

public class HomeController(RoutineDb db) : Controller
{
    private static DateOnly Today =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Asia/Kolkata").DateTime);

    private static TimeOnly NowTime =>
        TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Asia/Kolkata").DateTime);

    public async Task<IActionResult> Index(DateOnly? date, string? search, string status = "all")
    {
        var day = date ?? Today;
        var weekStart = day.AddDays(-Math.Min(day.DayNumber, ((int)day.DayOfWeek + 6) % 7));
        var weekEnd = weekStart.AddDays(Math.Min(6, DateOnly.MaxValue.DayNumber - weekStart.DayNumber));
        var month = new DateOnly(day.Year, day.Month, 1);
        var last = new DateOnly(day.Year, day.Month, DateTime.DaysInMonth(day.Year, day.Month));

        var monthDates = await db.Tasks
            .AsNoTracking()
            .Where(t => t.Date >= month && t.Date <= last)
            .Select(t => t.Date)
            .ToListAsync();

        var weekTasks = await db.Tasks
            .AsNoTracking()
            .Where(t => t.Date >= weekStart && t.Date <= weekEnd)
            .ToListAsync();

        var tasks = await db.Tasks
            .AsNoTracking()
            .Where(t => t.Date == day)
            .OrderBy(t => t.StartTime)
            .ThenBy(t => t.Id)
            .ToListAsync();

        var dashboard = new Dashboard
        {
            Date = day,
            Search = search,
            Status = status,
            Today = Today,
            Now = NowTime,
            WeekTasks = weekTasks,
            Tasks = tasks,
            MonthCounts = monthDates.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count())
        };

        return View(dashboard);
    }

    public IActionResult Create(DateOnly? date) =>
        View("Edit", new RoutineTask { Date = date ?? Today });

    public async Task<IActionResult> Edit(int id)
    {
        var task = await db.Tasks.FindAsync(id);
        return task == null ? NotFound() : View(task);
    }

    [HttpPost]
    public async Task<IActionResult> Save([Bind("Id,Title,Notes,Date,StartTime,EndTime,Category,Priority")] RoutineTask input, string? search, string status = "all")
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", input);
        }

        if (input.Id == 0)
        {
            db.Tasks.Add(input);
        }
        else
        {
            var task = await db.Tasks.FindAsync(input.Id);
            if (task == null)
            {
                return NotFound();
            }

            task.Title = input.Title.Trim();
            task.Notes = input.Notes;
            task.Date = input.Date;
            task.StartTime = input.StartTime;
            task.EndTime = input.EndTime;
            task.Category = input.Category;
            task.Priority = input.Priority;
        }

        await db.SaveChangesAsync();
        TempData["Message"] = "Task saved.";

        return RedirectToAction(nameof(Index), new { date = input.Date.ToString("yyyy-MM-dd"), search, status });
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id, string? search, string status = "all")
    {
        var task = await db.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        task.IsCompleted = !task.IsCompleted;
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { date = task.Date.ToString("yyyy-MM-dd"), search, status });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, string? search, string status = "all")
    {
        var task = await db.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        var day = task.Date;
        db.Tasks.Remove(task);
        await db.SaveChangesAsync();

        TempData["Message"] = "Task deleted.";
        return RedirectToAction(nameof(Index), new { date = day.ToString("yyyy-MM-dd"), search, status });
    }

    [HttpPost]
    public async Task<IActionResult> CompleteDay(DateOnly date, string? search, string status = "all")
    {
        if (!ModelState.IsValid || date == default)
        {
            return BadRequest("Invalid date.");
        }

        var pending = await db.Tasks
            .Where(t => t.Date == date && !t.IsCompleted)
            .ToListAsync();

        foreach (var task in pending)
        {
            task.IsCompleted = true;
        }

        await db.SaveChangesAsync();
        TempData["Message"] = pending.Count > 0
            ? $"Marked {pending.Count} tasks complete."
            : "All tasks for this day are already complete.";

        return RedirectToAction(nameof(Index), new { date = date.ToString("yyyy-MM-dd"), search, status });
    }

    [HttpPost]
    public async Task<IActionResult> CopyDay(DateOnly source, DateOnly target)
    {
        if (!ModelState.IsValid || source == default || target == default || source == target)
        {
            TempData["Message"] = "Choose a different valid destination date.";
            return RedirectToAction(nameof(Index));
        }

        if (await db.Tasks.AnyAsync(t => t.Date == target))
        {
            TempData["Message"] = "Destination already has tasks. Choose an empty day to avoid duplicates.";
            return RedirectToAction(nameof(Index), new { date = source.ToString("yyyy-MM-dd") });
        }

        var tasks = await db.Tasks
            .AsNoTracking()
            .Where(t => t.Date == source)
            .ToListAsync();

        var copies = tasks.Select(t => new RoutineTask
        {
            Title = t.Title,
            Notes = t.Notes,
            Date = target,
            StartTime = t.StartTime,
            EndTime = t.EndTime,
            Category = t.Category,
            Priority = t.Priority
        });

        db.Tasks.AddRange(copies);
        await db.SaveChangesAsync();

        TempData["Message"] = tasks.Count == 0 ? "No tasks to copy." : "Routine copied. Completion starts fresh.";
        return RedirectToAction(nameof(Index), new { date = target.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    public async Task<IActionResult> Starter(DateOnly date)
    {
        if (!ModelState.IsValid || date == default)
        {
            return BadRequest("Invalid date.");
        }

        if (!await db.Tasks.AnyAsync(t => t.Date == date))
        {
            var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            void Add(string title, string start, string end, string category) =>
                db.Tasks.Add(new RoutineTask
                {
                    Title = title,
                    Date = date,
                    StartTime = TimeOnly.ParseExact(start, "HH:mm", CultureInfo.InvariantCulture),
                    EndTime = TimeOnly.ParseExact(end, "HH:mm", CultureInfo.InvariantCulture),
                    Category = category
                });

            Add("Morning exercise & breakfast", "06:00", "07:00", "Exercise");
            Add("Theory revision", "07:00", "08:00", "Study");
            Add(weekend ? "Project practice" : "College", "08:00", "10:30", weekend ? "Study" : "College");
            Add("Review notes", "10:30", "11:30", "Study");
            Add("Lunch & rest", "11:30", "12:30", "Meals");
            Add("Coding practical", "12:30", "14:30", "Study");
            Add("Break & walk", "14:30", "15:00", "Personal");
            Add("Assignments", "15:00", "17:00", "Study");
            Add("Free time", "17:00", "19:00", "Personal");
            Add("Dinner", "19:00", "19:30", "Meals");
            Add("Revision & planning", "19:30", "21:00", "Study");
            Add("Relax & prepare for sleep", "21:00", "22:30", "Personal");

            await db.SaveChangesAsync();
            TempData["Message"] = "Starter routine added. Edit it to fit your day.";
        }
        else
        {
            TempData["Message"] = "Starter routines can only be added to an empty day.";
        }

        return RedirectToAction(nameof(Index), new { date = date.ToString("yyyy-MM-dd") });
    }

    public async Task<IActionResult> Export(DateOnly? date)
    {
        var day = date ?? Today;
        var tasks = await db.Tasks
            .AsNoTracking()
            .Where(t => t.Date == day)
            .OrderBy(t => t.StartTime)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Daily routine");
        var headers = new[] { "Date", "Task", "Start", "End", "Category", "Priority", "Completed", "Notes" };

        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        var row = 2;
        foreach (var task in tasks)
        {
            sheet.Cell(row, 1).Value = task.Date.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 1).Style.DateFormat.Format = "yyyy-mm-dd";

            // Value writes user text as text, rather than as an Excel formula.
            sheet.Cell(row, 2).Value = task.Title;
            sheet.Cell(row, 3).Value = task.StartTime.ToTimeSpan().TotalDays;
            sheet.Cell(row, 4).Value = task.EndTime.ToTimeSpan().TotalDays;
            sheet.Range(row, 3, row, 4).Style.NumberFormat.Format = "hh:mm";
            sheet.Cell(row, 5).Value = task.Category;
            sheet.Cell(row, 6).Value = task.Priority;
            sheet.Cell(row, 7).Value = task.IsCompleted ? "Yes" : "No";
            sheet.Cell(row, 8).Value = task.Notes ?? string.Empty;
            row++;
        }

        sheet.Range(1, 1, 1, 8).Style.Font.Bold = true;
        sheet.Range(1, 1, 1, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#287B66");
        sheet.Range(1, 1, 1, 8).Style.Font.FontColor = XLColor.White;
        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, Math.Max(1, row - 1), 8).SetAutoFilter();
        sheet.Columns(1, 8).Width = 18;
        sheet.Column(2).Width = 38;
        sheet.Column(8).Width = 55;
        sheet.Column(8).Style.Alignment.WrapText = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"routine-{day:yyyy-MM-dd}.xlsx");
    }

    public IActionResult Error() =>
        Problem("Something went wrong. Please try again.");
}
