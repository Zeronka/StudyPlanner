using StudyPlanner.Components;
using StudyPlanner.Models;
using StudyPlanner.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TaskRepository>();
builder.Services.AddHttpClient();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
var app = builder.Build();

// Request log middleware: method, path, status and duration; no request bodies.
app.Use(async (context, next) =>
{
    var timer = System.Diagnostics.Stopwatch.StartNew();
    var status = StatusCodes.Status500InternalServerError;
    try { await next(context); status = context.Response.StatusCode; }
    finally
    {
        app.Logger.LogInformation("HTTP {Method} {Path} status={Status} elapsed={Elapsed}ms",
            context.Request.Method, context.Request.Path,
            status, timer.ElapsedMilliseconds);
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/api/tasks", async (TaskRepository repository, string? subject, bool? completed) =>
{
    IEnumerable<StudyTask> tasks = await repository.GetAllAsync();
    if (!string.IsNullOrWhiteSpace(subject))
        tasks = tasks.Where(t => t.Subject.Equals(subject, StringComparison.OrdinalIgnoreCase));
    if (completed.HasValue) tasks = tasks.Where(t => t.IsCompleted == completed.Value);
    return Results.Ok(tasks.OrderBy(t => t.DueDate ?? DateOnly.MaxValue).ThenBy(t => t.Id));
});

app.MapGet("/api/tasks/{id:int}", async (int id, TaskRepository repository) =>
{
    var task = (await repository.GetAllAsync()).FirstOrDefault(t => t.Id == id);
    return task is null ? Results.NotFound(new { message = "Задача не найдена." }) : Results.Ok(task);
});

app.MapPost("/api/tasks", async (TaskInput input, TaskRepository repository) =>
{
    var errors = Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var task = await repository.CreateAsync(input);
    return Results.Created($"/api/tasks/{task.Id}", task);
});

app.MapPut("/api/tasks/{id:int}", async (int id, TaskInput input, TaskRepository repository) =>
{
    var errors = Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var task = await repository.UpdateAsync(id, input);
    return task is null ? Results.NotFound(new { message = "Задача не найдена." }) : Results.Ok(task);
});

app.MapDelete("/api/tasks/{id:int}", async (int id, TaskRepository repository) =>
    await repository.DeleteAsync(id) ? Results.NoContent()
        : Results.NotFound(new { message = "Задача не найдена." }));

app.MapGet("/api/stats", async (TaskRepository repository) =>
{
    var tasks = await repository.GetAllAsync();
    return Results.Ok(new { total = tasks.Length, completed = tasks.Count(t => t.IsCompleted),
        remaining = tasks.Count(t => !t.IsCompleted) });
});

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

static Dictionary<string, string[]> Validate(TaskInput input)
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Trim().Length > 120)
        errors["title"] = ["Название должно содержать от 1 до 120 символов."];
    if (string.IsNullOrWhiteSpace(input.Subject) || input.Subject.Trim().Length > 80)
        errors["subject"] = ["Предмет должен содержать от 1 до 80 символов."];
    return errors;
}
