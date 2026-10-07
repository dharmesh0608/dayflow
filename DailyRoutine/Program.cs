using DailyRoutine.Data;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Set ConnectionStrings:DefaultConnection for your MySQL server.");

builder.Services.AddDbContext<RoutineDb>(options =>
{
    options.UseMySQL(connectionString);
});

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<RoutineDb>().Database.EnsureCreated();
}
catch (Exception ex)
{
    app.Logger.LogCritical(
        ex,
        "MySQL Connection Warning: Unable to initialize or verify MySQL database during startup. " +
        "Ensure MySQL Server is running on port 3306 and credentials in appsettings.json are correct.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
