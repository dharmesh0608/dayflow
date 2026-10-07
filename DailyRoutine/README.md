# Dayflow — ASP.NET Core Daily Routine

A personal, local ASP.NET Core MVC website with MySQL persistence.

## Run on Windows (VS Code terminal or PowerShell)
1. Install the .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
2. Extract this project. Open the DailyRoutine folder in VS Code.
3. Install and start MySQL Server 8.0 or 8.4. Workbench is a client; you also need the running server.
4. Open `setup-mysql.sql` in MySQL Workbench using an administrator connection. Replace `CHANGE_ME` with your own password, then execute it. It creates `daily_routine_db` and a dedicated local user.
5. Set the same password in `appsettings.json` under `ConnectionStrings:DefaultConnection` (use JSON escaping for backslashes or quotes). Alternatively, keep it out of project files with .NET user secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=daily_routine_db;User=daily_routine_user;Password=YOUR_PASSWORD;"
```

6. Run:

```powershell
dotnet restore
dotnet run
```

7. Open http://localhost:5001

If the port is busy: `dotnet run --no-launch-profile --urls http://localhost:5002` and open port 5002.
First startup uses EnsureCreated to create the Tasks table in the empty MySQL database. No EF command is needed. Use a dedicated database, since EnsureCreated does not create missing tables when other tables already exist.

## Features
- Add, edit, delete and complete timed tasks with categories, notes and priorities.
- Calendar navigation and date picker, including historical dates.
- Per-date progress, completed count and total scheduled task hours.
- Search by title or notes; filter pending/completed tasks.
- Copy a routine to an empty date; copied tasks begin incomplete.
- Starter student routine 06:00–22:00, weekday college 08:00–10:30, lunch 11:30–12:30, dinner 19:00–19:30; weekends use project practice instead of college.
- Excel (.xlsx) export for the selected date.
- Responsive layout and server-side validation with antiforgery-protected forms.

## Data and behavior
All tasks have a specific date. Completing today's task does not change another day's completion. Copying is explicit, not automatic recurrence. Overlapping tasks are allowed; scheduled hours sum task durations and can count overlaps twice. Tasks cannot cross midnight: split them into two dated tasks. Dashboard's default date uses Asia/Kolkata. There are no notifications or background alarms.

This is a single-user local planner without login. Keep it on localhost. Add ASP.NET Core Identity and user ownership filters before shared hosting. HTTPS is not configured for this localhost demo.
Back up MySQL using Workbench Data Export. EnsureCreated is used for initial setup; later database schema changes need migrations or deliberate data migration, not deletion of saved data.

Existing SQLite data is NOT automatically transferred. Keep your original routine.db backup. This version starts with an empty MySQL database; copying the SQLite file into MySQL will not import it.

## Connection troubleshooting
- Connection refused: check that MySQL Server is running and the configured port is correct.
- Access denied: check user, password and the localhost grant; CREATE USER IF NOT EXISTS does not reset an existing password.
- Unknown database: run setup-mysql.sql first.
- Tasks table missing: use a dedicated empty database for first setup.
- User secrets are loaded in Development (the supplied launch profile uses Development). For other environments supply ConnectionStrings__DefaultConnection as an environment variable.
- The app is configured for localhost; configure HTTPS, authentication, ownership checks and AllowedHosts before shared hosting.

## Structure
Program.cs — application and database setup
Controllers/HomeController.cs — task actions, routine copying, export
Models/RoutineTask.cs — task, validation and dashboard model
Data/RoutineDb.cs — EF Core database context
Views/Home — planner and task editor
wwwroot — stylesheet and small confirmation script

## Manual acceptance checks
1. Run restore and run. Choose an empty date and add the starter routine.
2. Add a task. Invalid end-before-start time must show an error without saving.
3. Edit a task and confirm its values persist after restart.
4. Complete a task and verify progress changes; click again to undo.
5. Copy to an empty date. Confirm copies are incomplete and originals unchanged.
6. Copy to a populated date; confirm existing tasks are not overwritten.
7. Select a past date, search/filter tasks, export Excel (.xlsx), and delete a task.
8. Resize to a phone width and check that forms and calendar remain usable.

## Validation status
The authoring environment did not have the .NET SDK or a MySQL server, so this project has not been compiled or database-tested. Project XML, JSON settings and archive integrity were checked. Run the acceptance checks locally.

## References
https://dev.mysql.com/doc/connector-net/en/connector-net-entityframework-core-example.html
https://www.nuget.org/packages/MySql.EntityFrameworkCore/10.0.7
https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery

## Dashboard update
Added Today/Previous day/Next day buttons, starter routine shortcut, copy and Excel export shortcuts, current/next task cards (India time, calculated on page load), mark-day-complete with confirmation, Monday–Sunday progress and persistent browser theme selection. The starter routine now ends at 22:30. Existing tasks are not rewritten. No database schema changes are required.

Check the navigation buttons and selected date, bulk completion on a sample day, weekly totals after toggling a task, current/next task cards on Today, and theme persistence after refresh. These changes remain uncompiled because the authoring environment has no .NET SDK. Current/next cards refresh when you reload the page; there are no background alarms.
