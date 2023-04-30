using Microsoft.Extensions.Configuration;
using System.CommandLine;
using todo;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var authInformation = config.GetSection("AzureADInfo").Get<AzureADOauth>();

var rootCommand = new RootCommand(description: "Unofficial CLI for To-Do.");

var api = new PipeOutput();

var interactiveOption = new Option("--interactive",
    getDefaultValue: () => true);
rootCommand.AddGlobalOption(interactiveOption);


var logoutCommand = new Command("logout");
rootCommand.Add(logoutCommand);

SetupAddCommand(rootCommand);
SetupDeleteCommand(rootCommand);
SetupCheckTaskCommand(rootCommand);
SetupUncheckTaskCommand(rootCommand);

var listCommand = new Command("list", "");
var listNameArgument = new Argument<string>("list name");
listCommand.AddArgument(listNameArgument);
SetupAddCommand(listCommand);
SetupDeleteCommand(listCommand);
SetupCheckTaskCommand(listCommand);
SetupUncheckTaskCommand(listCommand);

rootCommand.Add(listCommand);

var myDayCommand = new Command("myday", "Tasks to tackle today!");
rootCommand.Add(myDayCommand);
myDayCommand.SetHandler(async () =>
{
    await api.ShowList("My Day");
});

var importartCommand = new Command("important");
rootCommand.Add(importartCommand);
myDayCommand.SetHandler(async () =>
{
    await api.ShowList("Important");
});

var plannedCommand = new Command("planned");
rootCommand.Add(plannedCommand);
myDayCommand.SetHandler(async () =>
{
    await api.ShowList("Planned");
});

var assignedCommand = new Command("assigned");
rootCommand.Add(assignedCommand);
myDayCommand.SetHandler(async () =>
{
    await api.ShowList("Assigned");
});

var tasksCommand = new Command("tasks", "List tasks");
var taskListName = new Argument("taskList", "Task list");
taskListName.SetDefaultValue("Tasks");
tasksCommand.SetHandler(async (String taskList) =>
{
    await api.ShowList(taskList);
}, taskListName);
rootCommand.Add(tasksCommand);

var loginCommand = new Command("login", "Log in with Microsoft Account (either personal or institutional account)");
var loginMethodOption = new Argument<Auth.LoginMethod>("method", () => Auth.LoginMethod.Code, "Way to allow notifications");

loginCommand.AddArgument(loginMethodOption);
loginCommand.SetHandler(async (Auth.LoginMethod loginMethodValue) =>
{
    await Auth.Login(authInformation, loginMethodValue);
    Console.WriteLine("Hello");
}, loginMethodOption);
rootCommand.Add(loginCommand);

return await rootCommand.InvokeAsync(args);

static void SetupAddCommand(Command command)
{
    var addCommand = new Command("add", "Create a new task");

    var taskTitleArgument = new Argument<string>("title", "Title of task.");
    addCommand.AddArgument(taskTitleArgument);

    var stepOption = new Option("--step")
    {
        Arity = ArgumentArity.ZeroOrMore
    };
    addCommand.AddOption(stepOption);

    var dateOption = new Option("--date", "Due date for task")
    {
        Arity = ArgumentArity.ZeroOrOne
    };
    addCommand.AddOption(dateOption);

    var notesOption = new Option<string>("--notes")
    {
        Arity = ArgumentArity.ZeroOrOne
    };
    addCommand.AddOption(notesOption);

    var addToMyDayOption = new Option<bool>("--add-to-day", getDefaultValue: () => false);
    addCommand.AddOption(addToMyDayOption);

    var remindOption = new Option("--remind")
    {
        Arity = ArgumentArity.ZeroOrOne
    };
    addCommand.AddOption(remindOption);

    var fileOption = new Option<FileInfo>("--file")
    {
        Arity = ArgumentArity.ZeroOrMore
    };
    addCommand.AddOption(fileOption);

    addCommand.SetHandler(async (string name, string date, string notes, bool addToMyDay, 
        string remindOption, string? fileOption) =>
    {
        //await api.AddTask(name, "Task");
    },
    taskTitleArgument, dateOption, notesOption, addToMyDayOption, remindOption, fileOption);
    command.Add(addCommand);
}

static void SetupDeleteCommand(Command rootCommand)
{
    var deleteCommand = new Command("delete", "Delete task");

    var taskNameArgument = new Argument<string>("task name", "Name of the task.");
    deleteCommand.AddArgument(taskNameArgument);

    deleteCommand.SetHandler(async (String name) =>
    {
        //await api.DeleteTask(name, "Task");
    },
    taskNameArgument);

    rootCommand.Add(deleteCommand);
}

static void SetupCheckTaskCommand(Command rootCommand)
{
    var checkCommand = new Command("check", "Marks task as finished");
    rootCommand.Add(checkCommand);
}

static void SetupUncheckTaskCommand(Command rootCommand)
{
    var unCheckCommand = new Command("uncheck", "Marks task as unfinished");
    rootCommand.Add(unCheckCommand);
}