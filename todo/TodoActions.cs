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

    public async Task DeleteTask(string listName, string taskTitle)
    {
        var listId = await api.GetListId(listName) ?? throw new Exception("List couldn't be found");
        var taskId = await api.GetTaskId(taskTitle, listId!) ?? throw new Exception("Task couldn't be found.");

        await api.DeleteTask(listId!, taskId!);
    }

    public async Task<TodoTask?> CreateTask(string title, string listName, DateTime? dueDate = null,
        DateTime? reminder = null, string? notes = "")
    {
        var listId = await api.GetListId(listName) ?? throw new Exception("List couldn't be found");
        var dueDateTimeTimeZone = dueDate != null ? new DateTimeTimeZone
        {
            DateTime = dueDate?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            TimeZone = TimeZoneInfo.Local.StandardName
        } : null;
        var reminderDateTimeTimeZone = reminder != null ? new DateTimeTimeZone
        {
            DateTime = reminder?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            TimeZone = TimeZoneInfo.Local.StandardName
        } : null;
        // Microsoft graph asks for a time with a timezone, will be using system timezone
        //var reminderDateTimeZone = reminder.
        return await api.CreateTask(title, listId, reminderDateTimeTimeZone, dueDateTimeTimeZone, notes);
    }

    public async Task<TodoTask?> EditTask(string originalTitle, string listName, string? newTitle = null,
    TaskStatus? status = null, DateTime? dueDate = null, DateTime? reminder = null, 
    List<FileInfo>? fileUri = null, string? notes = "")
    {
        var listId = await api.GetListId(listName) ?? throw new Exception("List couldn't be found");
        var taskId = await api.GetTaskId(originalTitle, listId!) ?? throw new Exception("Task couldn't be found.");
        var dueDateTimeTimeZone = dueDate != null ?new DateTimeTimeZone
        {
            DateTime = dueDate?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            TimeZone = TimeZoneInfo.Local.StandardName
        } : null;
        var reminderDateTimeTimeZone = reminder != null ? new DateTimeTimeZone
        {
            DateTime = reminder?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            TimeZone = TimeZoneInfo.Local.StandardName
        } : null;
        return await api.EditTask(taskId, listId, newTitle, reminderDateTimeTimeZone, dueDateTimeTimeZone, fileUri, status, notes);
    }

    public Task<Microsoft.Graph.Models.TodoTaskListCollectionResponse?> GetAllLists(){
        return api.GetAvailableLists();
    }

    public Task<Microsoft.Graph.Models.TodoTaskList?> AddList(string listName){
        return api.AddTaskList(listName);
    }

    public async Task DeleteList(string listName){
        var listId = await api.GetListId(listName) ?? throw new Exception("List couldn't be found");
        
        await api.DeleteTaskList(listId);
    }

    public async Task<Microsoft.Graph.Models.TodoTaskCollectionResponse?> GetTasksInList(string listName){
        var listId = await api.GetListId(listName) ?? throw new Exception("List couldn't be found");

        return await api.GetTasksInList(listId);
    }
}