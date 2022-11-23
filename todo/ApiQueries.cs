using Microsoft.Graph;

namespace todo;

public class ApiQueries
{
    public readonly GraphServiceClient graphClient;

    public ApiQueries(GraphServiceClient graphClient)
    {
        this.graphClient = graphClient;
    }

    Task DeleteTask(string listId, string taskId)
    {
        return graphClient.Me.Todo.Lists[listId].Tasks[taskId]
            .Request()
            .DeleteAsync();
    }

    public async Task DeleteByName(string listName, string taskTitle)
    {
        var listId = await GetListId(listName);

        if (listId == null)
        {

        }

        var taskId = await GetTaskId(taskTitle, listId!);

        if(taskId == null)
        {

        }

        await DeleteTask(listId!, taskId!);
    }

    async Task<string?> GetListId(string name)
    {
        var lists = await GetAvailableLists();
        var listOfName = lists.FirstOrDefault(l => l.DisplayName == name.Trim());
        return listOfName?.Id;
    }

    async Task<string?> GetTaskId(string taskTitle, string listId)
    {
        var taskInList = await GetListAsync(listId);
        return taskInList.FirstOrDefault(t => t.Title == taskTitle)?.Id;
    }

    async Task<ITodoTaskListTasksCollectionPage> GetTasksInList(string listId)
    {
        var tasks = await graphClient.Me.Todo.Lists[listId].Tasks
            .Request()
            .GetAsync();

        return tasks;
    }

    public async Task<TodoTask> CreateTask(string title, string listName, List<string>? fileUri = null, 
        List<string>? checkListItems = null, string? notes = "", bool? remind = false)
    {
        var newTask = new TodoTask();
        newTask.Title = title;
        newTask.Body = new ItemBody
        {
            Content = notes
        };

        var listId = await GetListId(listName);

        //var checkListItemsForApi = checkListItems?.Select(ck => new ChecklistItem
        //{
        //    DisplayName = ck
        //});

        return await graphClient.Me.Todo.Lists[listId].Tasks
            .Request()
            .AddAsync(newTask);
    }

    Task<ITodoTaskListTasksCollectionPage> GetListAsyncById(string listId)
    {
        var tasks = graphClient.Me.Todo.Lists[listId].Tasks
        .Request()
        .GetAsync();

        return tasks; 
    }

    public async Task<ITodoTaskListTasksCollectionPage> GetListAsync(string listName)
    {
        var listId = await GetListId(listName);
        return await GetListAsyncById(listId);
    }

    public Task<ITodoListsCollectionPage> GetAvailableLists()
    {
        var lists = graphClient.Me.Todo.Lists
        .Request()
        .GetAsync();

        return lists;
    }

    public async Task AddTaskList(string name)
    {
        var todoTaskList = new TodoTaskList
        {
            DisplayName = name
        };

        await graphClient.Me.Todo.Lists
            .Request()
            .AddAsync(todoTaskList);
    }

    public async Task DeleteTaskListByName(string name)
    {
        var id = await GetListId(name);
        if(id == null)
        {

        }

        await DeleteTaskList(id!);
    }

    Task DeleteTaskList(string taskListId)
    {
        return graphClient.Me.Todo.Lists[taskListId]
            .Request()
            .DeleteAsync();
    }

    internal Task CheckTask(string listName, string taskName)
    {
        throw new NotImplementedException();
    }
}
