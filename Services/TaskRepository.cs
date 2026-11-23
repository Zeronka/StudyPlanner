using System.Text.Json;
using StudyPlanner.Models;

namespace StudyPlanner.Services;

// Singleton: one shared store. SemaphoreSlim serializes reads and writes.
public sealed class TaskRepository
{
    private readonly string _file;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private List<StudyTask> _tasks;

    public TaskRepository(IWebHostEnvironment env)
    {
        var directory = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(directory);
        _file = Path.Combine(directory, "tasks.json");
        _tasks = File.Exists(_file)
            ? JsonSerializer.Deserialize<List<StudyTask>>(File.ReadAllText(_file), _json)
                ?? throw new InvalidDataException("Файл задач содержит null вместо списка.")
            : [new(1, "Повторить определения по психологии", "Психология", new(2026, 12, 2), false),
               new(2, "Разобрать HTTP-методы", "ASP.NET", new(2026, 12, 7), false)];
        if (_tasks.Any(t => t.Id < 1) || _tasks.Select(t => t.Id).Distinct().Count() != _tasks.Count)
            throw new InvalidDataException("В файле задач повторяются или неверны идентификаторы.");
    }

    public async Task<StudyTask[]> GetAllAsync()
    {
        await _gate.WaitAsync();
        try { return _tasks.ToArray(); }
        finally { _gate.Release(); }
    }

    public async Task<StudyTask> CreateAsync(TaskInput input)
    {
        await _gate.WaitAsync();
        try
        {
            var id = _tasks.Count == 0 ? 1 : checked(_tasks.Max(t => t.Id) + 1);
            var task = new StudyTask(id, input.Title!.Trim(), input.Subject!.Trim(), input.DueDate, input.IsCompleted);
            var next = new List<StudyTask>(_tasks) { task };
            await SaveAsync(next);
            _tasks = next;
            return task;
        }
        finally { _gate.Release(); }
    }

    public async Task<StudyTask?> UpdateAsync(int id, TaskInput input)
    {
        await _gate.WaitAsync();
        try
        {
            var index = _tasks.FindIndex(t => t.Id == id);
            if (index < 0) return null;
            var task = new StudyTask(id, input.Title!.Trim(), input.Subject!.Trim(), input.DueDate, input.IsCompleted);
            var next = new List<StudyTask>(_tasks);
            next[index] = task;
            await SaveAsync(next);
            _tasks = next;
            return task;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await _gate.WaitAsync();
        try
        {
            if (!_tasks.Any(t => t.Id == id)) return false;
            var next = _tasks.Where(t => t.Id != id).ToList();
            await SaveAsync(next);
            _tasks = next;
            return true;
        }
        finally { _gate.Release(); }
    }

    private async Task SaveAsync(List<StudyTask> next)
    {
        // Save a new file before changing live state; an unsuccessful write keeps the old list.
        var temporary = _file + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(next, _json));
        File.Move(temporary, _file, overwrite: true);
    }
}
