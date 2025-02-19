using todo;
using System.CommandLine;
using Microsoft.Extensions.Configuration;

class Program
{
    static AzureADOauth? authInformation;
    
    static async Task<int> Main(string[] args)
    {
#pragma region ApiSetup        
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        authInformation = config.GetSection("AzureADInfo").Get<AzureADOauth>();
        var authResult = await Auth.SignInSilently(authInformation!);
        var graphClient = Auth.CreateGraphClient(authResult);
        var apiQueries = new ApiQueries(graphClient);
        var todoActions = new TodoActions(apiQueries);
#pragma endregion ApiSetup

        var rootCommand = new RootCommand(description: "Unofficial CLI for To-Do.");

#pragma region AddCommand
        var addCommand = new Command("add", "Create a task.");
        var taskTitleArgument = new Argument<string>("task", "Task description");
        addCommand.Add(taskTitleArgument);
        var dueDateOption = new Option<DateTime>("--due-date", "A due date for task.");
        addCommand.Add(dueDateOption);

        var remindDateOption = new Option<DateTime>("--reminder", "At this time an alert will be sent to remind you of this task. This alert will be shown by one of the official apps, either on desktop or on your phone.");
        addCommand.Add(remindDateOption);

        var attachmentOption = new Option<List<FileInfo>>("--file", "Attachments to be added to task");

        var notesOption = new Option<string>("--notes", "Aditional notes for the task");
        addCommand.Add(notesOption);

        //var checklist = new Option<string>("--checklist", "Steps to complete this task. Or extra things that need to be done.");

        addCommand.SetHandler(async (task, dueDate, remindDate, attachments, notes) =>
        {
            await todoActions.CreateTask(task, "Tasks", dueDate, remindDate, attachments, notes);
        }, taskTitleArgument, dueDateOption, remindDateOption, attachmentOption, notesOption);
        rootCommand.Add(addCommand);
#pragma endregion AddCommand

#pragma region CheckCommand
        var checkCommand = new Command("check", "Mark a task as done");
        var checkTaskArgument = new Argument<string>("task", "Task title.");
        checkCommand.Add(checkTaskArgument);
        checkCommand.SetHandler<string>(async (task) =>
        {
            await todoActions.EditTask(task, "Tasks", status:Microsoft.Graph.Models.TaskStatus.Completed);
        }, checkTaskArgument);
        rootCommand.Add(checkCommand);
#pragma endregion CheckCommand

#pragma region UncheckCommand
        var uncheckCommand = new Command("uncheck", "Mark a task as not done");
        var uncheckTaskArgument = new Argument<string>("task", "Task title.");
        uncheckCommand.Add(uncheckTaskArgument);
        uncheckCommand.SetHandler<string>(async (task) =>
        {
            await todoActions.EditTask(task, "Tasks", status:Microsoft.Graph.Models.TaskStatus.NotStarted);
        }, uncheckTaskArgument);
        rootCommand.Add(uncheckCommand);
#pragma endregion UncheckCommand

#pragma region DeleteCommand
        var deleteCommand = new Command("delete", "Delete a task");
        var deleteTaskArgument = new Argument<string>("task", "Task title.");
        deleteCommand.Add(deleteTaskArgument);
        deleteCommand.SetHandler<string>(async (task) =>
        {
            await todoActions.DeleteTask("Tasks", task);
        }, deleteTaskArgument);
        rootCommand.Add(deleteCommand);
#pragma endregion DeleteCommand

#pragma region TasksInListCommand
        var tasksCommand = new Command("tasks", "Show tasks in list");
        var listNameArgument = new Argument<string>("listName", () => "Tasks", "Name of the list");
        tasksCommand.Add(listNameArgument);
        var listHiddenOption = new Option<bool>("--show-completed", () => false, "Show completed tasks in list");
        tasksCommand.Add(listHiddenOption);

        tasksCommand.SetHandler<string, bool>(async (listName, showCheckedTasks) =>
        {
            // Perform operations on the specified list
            await todoActions.GetTasksInList(listName);
        }, listNameArgument, listHiddenOption);
        rootCommand.Add(tasksCommand);
#pragma endregion TasksInListCommand

#pragma region ListCommand
        var showListsCommand = new Command("lists", "Show all lists.");
        showListsCommand.SetHandler(async () => {
            await todoActions.GetAllLists();
        });
        rootCommand.Add(showListsCommand);

#pragma endregion ListCommand    

#pragma region AddListCommand
        var createListCommand = new Command("add", "Create a new list.");
        var addListNameArgument = new Argument<string>("listName", "Name of the list");
        createListCommand.SetHandler(async (listName) => {
            await todoActions.AddList(listName);
        }, addListNameArgument);
        showListsCommand.Add(createListCommand);
#pragma endregion AddListCommand

#pragma region DeleteListCommand
        var deleteListCommand = new Command("delete", "Delete a list.");
        var deleteListNameArgument = new Argument<string>("listName", "Name of the list");
        deleteListCommand.SetHandler(async (listName) => {
            await todoActions.DeleteList(listName);
        }, deleteListNameArgument);
        showListsCommand.Add(deleteListCommand);
#pragma endregion DeleteListCommand

#pragma region Aliases
        var mydayCommand = new Command("myday", "Show tasks in My Day list.");
        mydayCommand.SetHandler(async () => {
            await todoActions.GetTasksInList("My Day");
        });
        rootCommand.Add(mydayCommand);

        var importantComand = new Command("important", "Show tasks in Important list.");
        importantComand.SetHandler(async () => {
            await todoActions.GetTasksInList("Important");
        });
        rootCommand.Add(importantComand);

        var plannedCommand = new Command("planned", "Show tasks in Planned list.");
        plannedCommand.SetHandler(async () => {
            await todoActions.GetTasksInList("Planned");
        });
        rootCommand.Add(plannedCommand);

        var assignedCommand = new Command("assigned", "Show tasks in Assigned list.");
        assignedCommand.SetHandler(async () => {
            await todoActions.GetTasksInList("Assigned");
        });
        rootCommand.Add(assignedCommand);

#pragma endregion Aliases

        return await rootCommand.InvokeAsync(args);
    }
}
