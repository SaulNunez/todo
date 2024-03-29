﻿using todo;
using Microsoft.Graph;
using System.CommandLine;
using Microsoft.Extensions.Configuration;
using TaskStatus = Microsoft.Graph.TaskStatus;
using Option = System.CommandLine.Option;

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
        var dueDateOption = new Option<DateTime>("--due-date", "A due date for task.");
        addCommand.Add(dueDateOption);

        var remindDateOption = new Option<DateTime>("--reminder", "At this time an alert will be sent to remind you of this task.");
        addCommand.Add(remindDateOption);

        var notesOption = new Option<string>("--notes", "A small section where you can add whatever");
        addCommand.Add(notesOption);

        var attachments = new Option<FileInfo>("--file", "Attachments to be added to task");

        var step = new Option<string>("--step", "Steps to complete this task. Or extra things that need to be done.");

        addCommand.SetHandler<string, DateTime, DateTime, string>(async (task, dueDate, remindDate, notes) =>
        {
            await AddTaskToMicrosoftToDoAsync(task, "Tasks", dueDate, remindDate, notes);
        }, addTaskNameArgument, dueDateOption, remindDateOption, notesOption);
        rootCommand.Add(addCommand);

        var checkCommand = new Command("check", "Mark a task as done in Microsoft To-Do");
        var checkTaskArgument = new Argument<string>("task", "Task title to complete");
        checkCommand.Add(checkTaskArgument);
        checkCommand.SetHandler<string>(async (task) =>
        {
            await CheckTaskInMicrosoftToDoAsync(task, "Tasks");
        }, checkTaskArgument);
        rootCommand.Add(checkCommand);

        var uncheckCommand = new Command("uncheck", "Mark a task as not done in Microsoft To-Do");
        var uncheckTaskArgument = new Argument<string>("task", "Task title to uncheck");
        uncheckCommand.Add(uncheckTaskArgument);
        uncheckCommand.SetHandler<string>(async (task) =>
        {
            await UncheckTaskInMicrosoftToDoAsync(task, "Tasks");
        }, uncheckTaskArgument);
        rootCommand.Add(uncheckCommand);

        var deleteCommand = new Command("delete", "Delete a task from Microsoft To-Do");
        var deleteTaskArgument = new Argument<string>("task", "Task title to delete");
        deleteCommand.Add(deleteTaskArgument);
        deleteCommand.SetHandler<string>(async (task) =>
        {
            await DeleteTaskFromMicrosoftToDoAsync(task, "Tasks");
        }, deleteTaskArgument);
        rootCommand.Add(deleteCommand);

        var listCommand = new Command("list", "Perform operations on a specific list in Microsoft To-Do");
        var listNameArgument = new Argument<string>("listName", () => "Tasks", "Name of the list");
        listCommand.Add(listNameArgument);
        var listHiddenOption = new Option<bool>("--show-completed", () => false, "Show completed tasks in list");
        listCommand.Add(listHiddenOption);

        listCommand.SetHandler<string, bool>(async (listName, showCheckedTasks) =>
        {
            // Perform operations on the specified list
            await ListTasksInMicrosoftToDoAsync(listName, showCheckedTasks);
        }, listNameArgument, listHiddenOption);
        rootCommand.Add(listCommand);

        var showListsCommand = new Command("lists", "List all lists in Microsoft To-Do");
        showListsCommand.SetHandler(async () => {
            await ListAllListsInMicrosoftToDoAsync();
        });
        rootCommand.Add(showListsCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task AddTaskToMicrosoftToDoAsync(string task, string listName, DateTime dueDate = default, 
        DateTime reminderDate = default, string? notes = null)
    {
        var graphClient = await Auth.LoginUserAsync(authInformation!);

        var todoTask = new TodoTask
        {
            Title = task,
        };

        if(dueDate != default(DateTime)){
            todoTask.DueDateTime = DateTimeTimeZone.FromDateTime(dueDate!);
        }

        if(reminderDate != default(DateTime)){
            todoTask.ReminderDateTime = DateTimeTimeZone.FromDateTime(reminderDate!);
        }

        if(notes != null || notes == string.Empty){
            todoTask.Body = new ItemBody{
                Content = notes,
                ContentType = BodyType.Text
            };
        }

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

    static async Task ListTasksInMicrosoftToDoAsync(string listName, bool showCheckedTasks)
    {
        var graphClient = await Auth.LoginUserAsync(authInformation!);

        var uncheckedTasks = await graphClient.Me.Todo.Lists[listName].Tasks
        .Request()
        .Filter("status ne 'completed'")
        .GetAsync();

        var checkedTasks = await graphClient.Me.Todo.Lists[listName].Tasks
            .Request()
            .Filter("status eq 'completed'")
            .GetAsync();
        

        Console.WriteLine($"Tasks in list '{listName}':");

        foreach (var task in uncheckedTasks)
        {
            Console.WriteLine($"[ ] {task.Title}");
        }

        if(showCheckedTasks){
                foreach (var task in checkedTasks)
                {
                    Console.WriteLine($"[x] {task.Title}");
                }
        }
    }

    static async Task ListAllListsInMicrosoftToDoAsync()
    {
        var graphClient = await Auth.LoginUserAsync(authInformation!);

        var lists = await graphClient.Me.Todo.Lists.Request().GetAsync();
        Console.WriteLine("Lists in Microsoft To-Do:");

        foreach (var list in lists)
        {
            Console.WriteLine($"- {list.DisplayName}");
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
