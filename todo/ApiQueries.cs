using Microsoft.Graph;
using Microsoft.Graph.Models;
using TaskStatus = Microsoft.Graph.Models.TaskStatus;

namespace todo;

/// <summary>
/// A thin wrapper over the Microsoft Graph library.
/// Creates objects needed for calling the API, like the parameter body.
/// </summary>
public class ApiQueries
{
    public readonly GraphServiceClient graphClient;

    public ApiQueries(GraphServiceClient graphClient)
    {
        this.graphClient = graphClient;
    }

    public Task DeleteTask(string listId, string taskId)
    {
        return graphClient.Me.Todo.Lists[listId].Tasks[taskId].DeleteAsync();
    }

    public async Task<string?> GetListId(string name)
    {
        var lists = await GetAvailableLists();
        var listOfName = lists.Value.FirstOrDefault(l => l.DisplayName == name.Trim());
        return listOfName?.Id;
    }

    public async Task<string?> GetTaskId(string taskTitle, string listId)
    {
        var taskInList = await GetTasksInList(listId);
        return taskInList.Value.FirstOrDefault(t => t.Title == taskTitle)?.Id;
    }

    public async Task<TodoTaskCollectionResponse?> GetTasksInList(string listId)
    {
        return await graphClient.Me.Todo.Lists[listId].Tasks.GetAsync();
    }

    public async Task<TodoTask?> EditTask(string taskId, string listId, string? newTitle = null, 
        DateTimeTimeZone? reminder = null, DateTimeTimeZone? dueDate = null, 
        List<FileInfo>? fileUri = null,  TaskStatus? status = null, string? notes = null )
    {
        var existingTask = await graphClient.Me.Todo.Lists[listId].Tasks[taskId].GetAsync() ?? throw new Exception("No task found");
        if (newTitle != null){
            existingTask.Title = newTitle;
        }
        if(notes != null){
            existingTask.Body = new ItemBody
            {
                Content = notes
            };
        }

        if(reminder != null){
            existingTask.ReminderDateTime = reminder;
        }

        if(dueDate != null){
            existingTask.DueDateTime = dueDate;
        }

        if(status != null){
            existingTask.Status = status;
        }

        //var checkListItemsForApi = checkListItems?.Select(ck => new ChecklistItem
        //{
        //    DisplayName = ck
        //});

        return await graphClient.Me.Todo.Lists[listId]
            .Tasks[taskId]
            .PatchAsync(existingTask);
    }

    public Task<TodoTask?> CreateTask(string title, string listId, DateTimeTimeZone? reminder = null,
    DateTimeTimeZone? dueDate = null, List<FileInfo>? fileUri = null, string? notes = "")
    {
        var newTask = new TodoTask
        {
            Title = title,
            Body = new ItemBody
            {
                Content = notes
            },
            ReminderDateTime = reminder,
            DueDateTime = dueDate,
        };

        //var checkListItemsForApi = checkListItems?.Select(ck => new ChecklistItem
        //{
        //    DisplayName = ck
        //});

        return graphClient.Me.Todo.Lists[listId].Tasks.PostAsync(newTask);
    }

    public Task<TodoTaskListCollectionResponse?> GetAvailableLists()
    {
        return graphClient.Me.Todo.Lists.GetAsync();
    }

    public Task<TodoTaskList?> AddTaskList(string name)
    {
        var requestBody = new TodoTaskList
        {
            DisplayName = name
        };

        return graphClient.Me.Todo.Lists.PostAsync(requestBody);
    }

    public Task DeleteTaskList(string taskListId)
    {
        return graphClient.Me.Todo.Lists[taskListId].DeleteAsync();
    }
}
