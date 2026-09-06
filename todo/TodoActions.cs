using System.Globalization;
using Microsoft.Graph.Models;
using TaskStatus = Microsoft.Graph.Models.TaskStatus;

namespace todo;

/// <summary>
/// A wrapper before calling the API.
/// Does convertion of data types before calling the API wrapper.
/// Will also handle error conditions due to wrong parameters.
/// </summary>
public class TodoActions(ApiQueries api)
{
    private readonly ApiQueries api = api;

    /// <summary>
    /// Microsoft Graph wants a time plus a timezone name. Sending
    /// TimeZoneInfo.Local.StandardName only works where ICU supplies a Windows-style
    /// name; with InvariantGlobalization, or on a container without ICU, it degrades
    /// to an IANA id or abbreviation that Graph rejects. Converting to UTC and saying
    /// so is unambiguous everywhere.
    /// </summary>
    private static DateTimeTimeZone? ToGraphDateTime(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        return new DateTimeTimeZone
        {
            DateTime = value.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            TimeZone = "UTC"
        };
    }

    public async Task DeleteTask(string listName, string taskTitle)
    {
        var listId = await api.GetListId(listName) ?? throw new TodoCliException($"List \"{listName}\" couldn't be found.");
        var taskId = await api.GetTaskId(taskTitle, listId) ?? throw new TodoCliException($"Task \"{taskTitle}\" couldn't be found in \"{listName}\".");

        await api.DeleteTask(listId, taskId);
    }

    public async Task<TodoTask?> CreateTask(string title, string listName, DateTime? dueDate = null,
        DateTime? reminder = null, string? notes = null)
    {
        var listId = await api.GetListId(listName) ?? throw new TodoCliException($"List \"{listName}\" couldn't be found.");
        return await api.CreateTask(title, listId, ToGraphDateTime(reminder), ToGraphDateTime(dueDate), notes);
    }

    // notes defaults to null, not "": a null means "leave the notes alone". It used to
    // default to "", so check/uncheck - which never pass notes - wiped the task's notes.
    public async Task<TodoTask?> EditTask(string originalTitle, string listName, string? newTitle = null,
    TaskStatus? status = null, DateTime? dueDate = null, DateTime? reminder = null, 
    List<FileInfo>? fileUri = null, string? notes = null)
    {
        var listId = await api.GetListId(listName) ?? throw new TodoCliException($"List \"{listName}\" couldn't be found.");
        var taskId = await api.GetTaskId(originalTitle, listId) ?? throw new TodoCliException($"Task \"{originalTitle}\" couldn't be found in \"{listName}\".");

        return await api.EditTask(taskId, listId, newTitle, ToGraphDateTime(reminder), ToGraphDateTime(dueDate), fileUri, status, notes);
    }

    public Task<List<TodoTaskList>> GetAllLists(){
        return api.GetAvailableLists();
    }

    public Task<TodoTaskList?> AddList(string listName){
        return api.AddTaskList(listName);
    }

    public async Task DeleteList(string listName){
        var listId = await api.GetListId(listName) ?? throw new TodoCliException($"List \"{listName}\" couldn't be found.");

        await api.DeleteTaskList(listId);
    }

    public async Task<List<TodoTask>> GetTasksInList(string listName){
        var listId = await api.GetListId(listName) ?? throw new TodoCliException($"List \"{listName}\" couldn't be found.");

        return await api.GetTasksInList(listId);
    }
}
