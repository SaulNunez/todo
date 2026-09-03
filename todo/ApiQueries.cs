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
    /// <summary>
    /// Stops a malformed @odata.nextLink chain from looping forever.
    /// </summary>
    private const int MaxPages = 100;

    private readonly GraphServiceClient graphClient;

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
        // An empty name would make the matching below accept every list, which
        // silently targets an arbitrary list instead of failing.
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new TodoCliException("A list name is required.");
        }

        var wanted = name.Trim();
        var lists = await GetAvailableLists();

        // An exact title wins outright, so a list named "Work" is reachable even when
        // a "Work (archived)" list also exists.
        var exactMatch = lists.FirstOrDefault(l => string.Equals(
            l.DisplayName?.Trim(), wanted, StringComparison.CurrentCultureIgnoreCase));
        if (exactMatch != null)
        {
            return exactMatch.Id;
        }

        // Otherwise fall back to a substring match, which is what makes emoji-decorated
        // titles usable. They are good for visual organization but a pain to type.
        var partialMatches = lists
            .Where(l => l.DisplayName?.Contains(wanted, StringComparison.CurrentCultureIgnoreCase) ?? false)
            .ToList();

        return partialMatches.Count switch
        {
            0 => null,
            1 => partialMatches[0].Id,
            _ => throw new TodoCliException(
                $"'{name}' matches {partialMatches.Count} lists: " +
                string.Join(", ", partialMatches.Select(l => $"\"{l.DisplayName}\"")) +
                ". Please use the exact list name.")
        };
    }

    public async Task<string?> GetTaskId(string taskTitle, string listId)
    {
        var tasksInList = await GetTasksInList(listId);
        var wanted = taskTitle.Trim();

        var exactMatch = tasksInList.FirstOrDefault(t => t.Title == taskTitle);
        if (exactMatch != null)
        {
            return exactMatch.Id;
        }

        var caseInsensitiveMatches = tasksInList
            .Where(t => string.Equals(t.Title?.Trim(), wanted, StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        return caseInsensitiveMatches.Count switch
        {
            0 => null,
            1 => caseInsensitiveMatches[0].Id,
            _ => throw new TodoCliException(
                $"'{taskTitle}' matches {caseInsensitiveMatches.Count} tasks in this list. " +
                "Please use the exact task title.")
        };
    }

    /// <summary>
    /// Returns every task in the list, following @odata.nextLink. Graph only returns
    /// one page at a time, so without this tasks past the first page are invisible
    /// to "todo tasks" and unfindable by check/uncheck/delete.
    /// </summary>
    public async Task<List<TodoTask>> GetTasksInList(string listId)
    {
        var tasks = new List<TodoTask>();
        var page = await graphClient.Me.Todo.Lists[listId].Tasks.GetAsync();

        for (var pageCount = 0; page?.Value != null && pageCount < MaxPages; pageCount++)
        {
            tasks.AddRange(page.Value);

            if (string.IsNullOrEmpty(page.OdataNextLink))
            {
                break;
            }

            page = await graphClient.Me.Todo.Lists[listId].Tasks
                .WithUrl(page.OdataNextLink)
                .GetAsync();
        }

        return tasks;
    }

    public async Task<TodoTask?> EditTask(string taskId, string listId, string? newTitle = null,
        DateTimeTimeZone? reminder = null, DateTimeTimeZone? dueDate = null,
        List<FileInfo>? fileUri = null,  TaskStatus? status = null, string? notes = null )
    {
        // PATCH only the fields being changed. Reading the task and sending the whole
        // entity back would also echo server-owned properties such as CreatedDateTime,
        // which costs an extra round-trip and can be rejected.
        var changes = new TodoTask();

        if (newTitle != null){
            changes.Title = newTitle;
        }
        if(notes != null){
            changes.Body = new ItemBody
            {
                Content = notes
            };
        }

        if(reminder != null){
            changes.ReminderDateTime = reminder;
        }

        if(dueDate != null){
            changes.DueDateTime = dueDate;
        }

        if(status != null){
            changes.Status = status;
        }

        //var checkListItemsForApi = checkListItems?.Select(ck => new ChecklistItem
        //{
        //    DisplayName = ck
        //});

        return await graphClient.Me.Todo.Lists[listId]
            .Tasks[taskId]
            .PatchAsync(changes);
    }

    public Task<TodoTask?> CreateTask(string title, string listId, DateTimeTimeZone? reminder = null,
    DateTimeTimeZone? dueDate = null, string? notes = null)
    {
        var newTask = new TodoTask
        {
            Title = title
        };

        if(notes != null)
        {
            newTask.Body = new ItemBody
            {
                Content = notes
            };
        }

        if(reminder != null)
        {
            newTask.ReminderDateTime = reminder;
        }
        
        if(dueDate != null) 
        {
            newTask.DueDateTime = dueDate;
        }

        //var checkListItemsForApi = checkListItems?.Select(ck => new ChecklistItem
        //{
        //    DisplayName = ck
        //});

        return graphClient.Me.Todo.Lists[listId].Tasks.PostAsync(newTask);
    }

    /// <summary>
    /// Returns every list, following @odata.nextLink for the same reason as
    /// <see cref="GetTasksInList"/>.
    /// </summary>
    public async Task<List<TodoTaskList>> GetAvailableLists()
    {
        var lists = new List<TodoTaskList>();
        var page = await graphClient.Me.Todo.Lists.GetAsync();

        for (var pageCount = 0; page?.Value != null && pageCount < MaxPages; pageCount++)
        {
            lists.AddRange(page.Value);

            if (string.IsNullOrEmpty(page.OdataNextLink))
            {
                break;
            }

            page = await graphClient.Me.Todo.Lists
                .WithUrl(page.OdataNextLink)
                .GetAsync();
        }

        return lists;
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
