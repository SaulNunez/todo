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

#pragma region TasksInListCommand
        var task = new Command("tasks", "Show tasks in list");
        var listNameArgument = new Argument<string>("listName", "Name of the list");
        task.Add(listNameArgument);
        //var listHiddenOption = new Option<bool>("--show-completed", () => false, "Show completed tasks in list");
        //tasksCommand.Add(listHiddenOption);

        task.SetHandler<string>(async (listName) =>
        {
            // Perform operations on the specified list
            var listOfTasks = await todoActions.GetTasksInList(listName);
            PrettyPrint.Print(listOfTasks);
        }, listNameArgument);
        rootCommand.Add(task);
#pragma endregion TasksInListCommand

#pragma region AddCommand
        var addCommand = new Command("add", "Create a task.");
        var taskTitleArgument = new Argument<string>("task", "Task description");
        addCommand.Add(taskTitleArgument);
        var dueDateOption = new Option<DateTime?>("--due-date", () => null, "A due date for task.");
        addCommand.Add(dueDateOption);

        var remindDateOption = new Option<DateTime?>("--reminder-date", () => null, "At this time an alert will be sent to remind you of this task. This alert will be shown by one of the official apps, either on desktop or on your phone.");
        addCommand.Add(remindDateOption);

        var notesOption = new Option<string>("--notes", "Aditional notes for the task");
        addCommand.Add(notesOption);

        //var checklist = new Option<string>("--checklist", "Steps to complete this task. Or extra things that need to be done.");

        addCommand.SetHandler(async (listName, task, dueDate, remindDate, notes) =>
        {
            var newTask = await todoActions.CreateTask(task, listName, dueDate, remindDate, notes);
            PrettyPrint.Print(newTask);
        }, listNameArgument, taskTitleArgument, dueDateOption, remindDateOption, notesOption);
        task.Add(addCommand);
#pragma endregion AddCommand

#pragma region CheckCommand
        var checkCommand = new Command("check", "Mark a task as done");
        var checkTaskArgument = new Argument<string>("task", "Task title.");
        checkCommand.Add(checkTaskArgument);
        checkCommand.SetHandler(async (listName, task) =>
        {
            var editedTask = await todoActions.EditTask(task, listName, status:Microsoft.Graph.Models.TaskStatus.Completed);
            PrettyPrint.Print(editedTask);
        }, listNameArgument, checkTaskArgument);
        task.Add(checkCommand);
#pragma endregion CheckCommand

#pragma region UncheckCommand
        var uncheckCommand = new Command("uncheck", "Mark a task as not done");
        var uncheckTaskArgument = new Argument<string>("task", "Task title.");
        uncheckCommand.Add(uncheckTaskArgument);
        uncheckCommand.SetHandler(async (listName, task) =>
        {
            var editedTask = await todoActions.EditTask(task, listName, status:Microsoft.Graph.Models.TaskStatus.NotStarted);
            PrettyPrint.Print(editedTask);
        }, listNameArgument, uncheckTaskArgument);
        task.Add(uncheckCommand);
#pragma endregion UncheckCommand

#pragma region DeleteCommand
        var deleteCommand = new Command("delete", "Delete a task");
        var deleteTaskArgument = new Argument<string>("task", "Task title.");
        deleteCommand.Add(deleteTaskArgument);
        deleteCommand.SetHandler(async (listName, task) =>
        {
            await todoActions.DeleteTask(listName, task);
        }, listNameArgument, deleteTaskArgument);
        task.Add(deleteCommand);
#pragma endregion DeleteCommand

#pragma region ListCommand
        var showListsCommand = new Command("lists", "Show all lists.");
        showListsCommand.SetHandler(async () => {
            var lists = await todoActions.GetAllLists();
            PrettyPrint.Print(lists);
        });
        rootCommand.Add(showListsCommand);

#pragma endregion ListCommand    

#pragma region AddListCommand
        var createListCommand = new Command("add", "Create a new list.");
        var addListNameArgument = new Argument<string>("listName", "Name of the list");
        createListCommand.SetHandler(async (listName) => {
            await todoActions.AddList(listName);
        }, addListNameArgument);
        createListCommand.Add(addListNameArgument);
        showListsCommand.Add(createListCommand);
#pragma endregion AddListCommand

#pragma region DeleteListCommand
        var deleteListCommand = new Command("delete", "Delete a list.");
        var deleteListNameArgument = new Argument<string>("listName", "Name of the list");
        deleteListCommand.SetHandler(async (listName) => {
            await todoActions.DeleteList(listName);
        }, deleteListNameArgument);
        deleteListCommand.Add(deleteListNameArgument);
        showListsCommand.Add(deleteListCommand);
#pragma endregion DeleteListCommand

        return await rootCommand.InvokeAsync(args);
    }
}
