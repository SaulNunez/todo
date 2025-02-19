using System.Globalization;
using Microsoft.Graph.Models;
using TaskStatus = Microsoft.Graph.Models.TaskStatus;

namespace todo;

/// <summary>
/// A wrapper before calling the API.
/// Does convertion of data types before calling the API wrapper.
/// Will also handle error conditions due to wrong parameters.
/// </summary>
public class TodoActions{
    private ApiQueries api;

    public TodoActions(ApiQueries api){
        this.api = api;
    }

    public async Task<Task> DeleteTask(string listName, string taskTitle)
    {
        var listId = await api.GetListId(listName);

        if (listId == null)
        {

        }

        var taskId = await api.GetTaskId(taskTitle, listId!);

        if(taskId == null)
        {

        }

        return api.DeleteTask(listId!, taskId!);
    }

    public async Task<Task<TodoTask?>> CreateTask(string title, string listName, DateTime? dueDate = null,
        DateTime? reminder = null, List<FileInfo>? fileUri = null, string? notes = "")
    {
        var listId = await api.GetListId(listName);
        var dueDateTimeTimeZone = new DateTimeTimeZone
        {
            DateTime = dueDate?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            TimeZone = TimeZoneInfo.Local.StandardName
        };
        var reminderDateTimeTimeZone = new DateTimeTimeZone
        {
            DateTime = reminder?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            TimeZone = TimeZoneInfo.Local.StandardName
        };
        // Microsoft graph asks for a time with a timezone, will be using system timezone
        //var reminderDateTimeZone = reminder.
        return api.CreateTask(title, listId, reminderDateTimeTimeZone, dueDateTimeTimeZone, fileUri, notes);
    }

    public async Task<Task<Task<TodoTask?>>> EditTask(string originalTitle, string listName, string? newTitle = null,
    TaskStatus? status = null, DateTime? dueDate = null, DateTime? reminder = null, 
    List<FileInfo>? fileUri = null, string? notes = "")
    {
        var listId = await api.GetListId(listName);
        var dueDateTimeTimeZone = new DateTimeTimeZone
        {
            DateTime = dueDate?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            TimeZone = TimeZoneInfo.Local.StandardName
        };
        var reminderDateTimeTimeZone = new DateTimeTimeZone
        {
            DateTime = reminder?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            TimeZone = TimeZoneInfo.Local.StandardName
        };
        return api.EditTask(originalTitle, newTitle, listId, reminderDateTimeTimeZone, dueDateTimeTimeZone, fileUri, status, notes);
    }

    public Task<Microsoft.Graph.Models.TodoTaskListCollectionResponse?> GetAllLists(){
        return api.GetAvailableLists();
    }

    public Task<Microsoft.Graph.Models.TodoTaskList?> AddList(string listName){
        return api.AddTaskList(listName);
    }

    public async Task<Task> DeleteList(string listName){
        var listId = await api.GetListId(listName);
        
        return api.DeleteTaskList(listId);
    }

    public async Task<Task<Microsoft.Graph.Models.TodoTaskCollectionResponse?>> GetTasksInList(string listName){
        var listId = await api.GetListId(listName);

        return api.GetTasksInList(listId);
    }
}