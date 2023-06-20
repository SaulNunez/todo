using todo;
using Microsoft.Graph;
using System.CommandLine;
using Microsoft.Extensions.Configuration;
using TaskStatus = Microsoft.Graph.TaskStatus;

class Program
{
    static AzureADOauth? authInformation;
    
    static async Task<int> Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        authInformation = config.GetSection("AzureADInfo").Get<AzureADOauth>();

        var rootCommand = new RootCommand(description: "Unofficial CLI for To-Do.");

        var addCommand = new Command("add", "Create a task in Microsoft To-Do");
        var addTaskNameArgument = new Argument<string>("task", "Task description");
        addCommand.Add(addTaskNameArgument);

        addCommand.SetHandler<string>(async (task) =>
        {
            await AddTaskToMicrosoftToDoAsync(task, "Tasks");
        }, addTaskNameArgument);
        rootCommand.Add(addCommand);

        var checkCommand = new Command("check", "Mark a task as done in Microsoft To-Do")
            {
                new Argument<string>("task", "Task title to complete"),
            };
        checkCommand.SetHandler<string>(async (task) =>
        {
            await CheckTaskInMicrosoftToDoAsync(task, "Tasks");
        });
        rootCommand.Add(checkCommand);

        var uncheckCommand = new Command("uncheck", "Mark a task as not done in Microsoft To-Do")
            {
                new Argument<string>("task", "Task title to uncheck"),
            };
        uncheckCommand.SetHandler<string>(async (task) =>
        {
            await UncheckTaskInMicrosoftToDoAsync(task, "Tasks");
        });
        rootCommand.Add(uncheckCommand);

        var deleteCommand = new Command("delete", "Delete a task from Microsoft To-Do")
            {
                new Argument<string>("task", "Task title to delete"),
            };
        deleteCommand.SetHandler<string>(async (task) =>
        {
            await DeleteTaskFromMicrosoftToDoAsync(task, "Tasks");
        });
        rootCommand.Add(deleteCommand);

        var listCommand = new Command("list", "Perform operations on a specific list in Microsoft To-Do")
            {
                new Argument<string>("listName", "Name of the list")
            };
        /*listCommand.SetHandler<string>(async (listName, command) =>
        {
            // Perform operations on the specified list
            await PerformListOperationsAsync(listName, command);
        });*/

        return await rootCommand.InvokeAsync(args);
    }

    static async Task AddTaskToMicrosoftToDoAsync(string task, string listName)
    {
        var graphClient = await Auth.LoginUserAsync(authInformation!);

        var todoTask = new TodoTask
        {
            Title = task
        };

        await graphClient.Me.Todo.Lists[listName].Tasks.Request().AddAsync(todoTask);
        Console.WriteLine($"Task '{task}' added to list '{listName}'.");
    }

    static async Task CheckTaskInMicrosoftToDoAsync(string taskTitle, string listName)
    {
        var graphClient = await Auth.LoginUserAsync(authInformation!);

        var queryOptions = new QueryOption("Filter", $"status ne 'completed' and title eq '{taskTitle}'");

        var todoTasks = await graphClient.Me.Todo.Lists[listName].Tasks.Request(new List<QueryOption>{queryOptions}).GetAsync();

        var taskToComplete = todoTasks.FirstOrDefault();
        if (taskToComplete != null)
        {
            taskToComplete.Status = TaskStatus.Completed;
            await graphClient.Me.Todo.Lists[listName].Tasks[taskToComplete.Id].Request().UpdateAsync(taskToComplete);
            Console.WriteLine($"Task '{taskTitle}' marked as completed in list '{listName}'.");
        }
        else
        {
            Console.WriteLine($"Task '{taskTitle}' not found in list '{listName}'.");
        }
    }

    static async Task UncheckTaskInMicrosoftToDoAsync(string taskTitle, string listName)
    {
        var graphClient = await Auth.LoginUserAsync(authInformation!);

        var queryOptions = new QueryOption("Filter", $"status eq 'completed' and title eq '{taskTitle}'");

        var todoTasks = await graphClient.Me.Todo.Lists[listName].Tasks.Request(new List<QueryOption>{queryOptions}).GetAsync();

        var taskToUncheck = todoTasks.FirstOrDefault();
        if (taskToUncheck != null)
        {
            taskToUncheck.Status = TaskStatus.NotStarted;
            await graphClient.Me.Todo.Lists[listName].Tasks[taskToUncheck.Id].Request().UpdateAsync(taskToUncheck);
            Console.WriteLine($"Task '{taskTitle}' marked as not completed in list '{listName}'.");
        }
        else
        {
            Console.WriteLine($"Task '{taskTitle}' not found in list '{listName}'.");
        }
    }

    static async Task DeleteTaskFromMicrosoftToDoAsync(string taskTitle, string listName)
    {
        var graphClient = await Auth.LoginUserAsync(authInformation!);

        var queryOptions = new QueryOption("Filter", $"title eq '{taskTitle}'");

        var todoTasks = await graphClient.Me.Todo.Lists[listName].Tasks.Request(new List<QueryOption>{queryOptions}).GetAsync();

        var taskToDelete = todoTasks.FirstOrDefault();
        if (taskToDelete != null)
        {
            await graphClient.Me.Todo.Lists[listName].Tasks[taskToDelete.Id].Request().DeleteAsync();
            Console.WriteLine($"Task '{taskTitle}' deleted successfully from list '{listName}'.");
        }
        else
        {
            Console.WriteLine($"Task '{taskTitle}' not found in list '{listName}'.");
        }
    }

    static async Task PerformListOperationsAsync(string listName, string command)
    {
        switch (command)
        {
            case "add":
                Console.WriteLine("Please provide a task to add.");
                break;
            case "check":
                Console.WriteLine("Please provide a task to check.");
                break;
            case "uncheck":
                Console.WriteLine("Please provide a task to uncheck.");
                break;
            case "delete":
                Console.WriteLine("Please provide a task to delete.");
                break;
            default:
                Console.WriteLine("Invalid command.");
                break;
        }
    }
}
