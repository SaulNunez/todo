using todo;
using todo.Tui;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Configuration;

class Program
{
    const string ClientIdPlaceholder = "REPLACE_WITH_APP_REGISTRATION_CLIENT_ID";

    static async Task<int> Main(string[] args)
    {
        // A bare "todo" in a real terminal opens the interactive UI. Piped or
        // redirected runs keep the plain usage output, so scripts are unaffected.
        if (args.Length == 0 && TodoTui.IsSupported)
        {
            args = ["ui"];
        }

        // Sign-in is deferred until a command actually needs it, so that --help,
        // --version and parse errors do not trigger a device-code login.
        var todoActions = new Lazy<Task<TodoActions>>(CreateTodoActionsAsync);

        var rootCommand = new RootCommand(description: "Unofficial CLI for To-Do.");

        #region TasksInListCommand
        var task = new Command("tasks", "Show tasks in list");
        var listNameArgument = new Argument<string>("listName", "Name of the list");
        listNameArgument.AddValidator(result =>
        {
            if (string.IsNullOrWhiteSpace(result.GetValueOrDefault<string>()))
            {
                result.ErrorMessage = "A list name is required, for example: todo tasks \"Shopping List\"";
            }
        });
        task.Add(listNameArgument);
        //var listHiddenOption = new Option<bool>("--show-completed", () => false, "Show completed tasks in list");
        //tasksCommand.Add(listHiddenOption);

        task.SetHandler<string>(async (listName) =>
        {
            // Perform operations on the specified list
            var listOfTasks = await (await todoActions.Value).GetTasksInList(listName);
            PrettyPrint.Print(listOfTasks);
        }, listNameArgument);
        rootCommand.Add(task);
        #endregion TasksInListCommand

        #region AddCommand
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
            var newTask = await (await todoActions.Value).CreateTask(task, listName, dueDate, remindDate, notes);
            PrettyPrint.Print(newTask);
        }, listNameArgument, taskTitleArgument, dueDateOption, remindDateOption, notesOption);
        task.Add(addCommand);
        #endregion AddCommand

        #region CheckCommand
        var checkCommand = new Command("check", "Mark a task as done");
        var checkTaskArgument = new Argument<string>("task", "Task title.");
        checkCommand.Add(checkTaskArgument);
        checkCommand.SetHandler(async (listName, task) =>
        {
            var editedTask = await (await todoActions.Value).EditTask(task, listName, status:Microsoft.Graph.Models.TaskStatus.Completed);
            PrettyPrint.Print(editedTask);
        }, listNameArgument, checkTaskArgument);
        task.Add(checkCommand);
        #endregion CheckCommand

        #region UncheckCommand
        var uncheckCommand = new Command("uncheck", "Mark a task as not done");
        var uncheckTaskArgument = new Argument<string>("task", "Task title.");
        uncheckCommand.Add(uncheckTaskArgument);
        uncheckCommand.SetHandler(async (listName, task) =>
        {
            var editedTask = await (await todoActions.Value).EditTask(task, listName, status:Microsoft.Graph.Models.TaskStatus.NotStarted);
            PrettyPrint.Print(editedTask);
        }, listNameArgument, uncheckTaskArgument);
        task.Add(uncheckCommand);
        #endregion UncheckCommand

        #region DeleteCommand
        var deleteCommand = new Command("delete", "Delete a task");
        var deleteTaskArgument = new Argument<string>("task", "Task title.");
        deleteCommand.Add(deleteTaskArgument);
        deleteCommand.SetHandler(async (listName, task) =>
        {
            await (await todoActions.Value).DeleteTask(listName, task);
        }, listNameArgument, deleteTaskArgument);
        task.Add(deleteCommand);
        #endregion DeleteCommand

        // The listName argument lives on "tasks", but the parser happily binds it to ""
        // when a subcommand token is seen first (todo tasks add "Buy milk"). An empty
        // name matches every list, so guard each subcommand at parse time.
        foreach (var subcommand in new[] { addCommand, checkCommand, uncheckCommand, deleteCommand })
        {
            subcommand.AddValidator(result =>
            {
                var listNameResult = result.FindResultFor(listNameArgument)
                    ?? result.Parent?.FindResultFor(listNameArgument);
                if (listNameResult is null)
                {
                    result.ErrorMessage =
                        $"A list name is required, for example: todo tasks \"Shopping List\" {subcommand.Name} ...";
                }
            });
        }

        #region ListCommand
        var showListsCommand = new Command("lists", "Show all lists.");
        showListsCommand.SetHandler(async () => {
            var lists = await (await todoActions.Value).GetAllLists();
            PrettyPrint.Print(lists);
        });
        rootCommand.Add(showListsCommand);

        #endregion ListCommand

        #region AddListCommand
        var createListCommand = new Command("add", "Create a new list.");
        var addListNameArgument = new Argument<string>("listName", "Name of the list");
        createListCommand.SetHandler(async (listName) => {
            await (await todoActions.Value).AddList(listName);
        }, addListNameArgument);
        createListCommand.Add(addListNameArgument);
        showListsCommand.Add(createListCommand);
        #endregion AddListCommand

        #region DeleteListCommand
        var deleteListCommand = new Command("delete", "Delete a list.");
        var deleteListNameArgument = new Argument<string>("listName", "Name of the list");
        deleteListNameArgument.AddValidator(result =>
        {
            if (string.IsNullOrWhiteSpace(result.GetValueOrDefault<string>()))
            {
                result.ErrorMessage = "A list name is required, for example: todo lists delete \"Shopping List\"";
            }
        });
        deleteListCommand.SetHandler(async (listName) => {
            await (await todoActions.Value).DeleteList(listName);
        }, deleteListNameArgument);
        deleteListCommand.Add(deleteListNameArgument);
        showListsCommand.Add(deleteListCommand);
        #endregion DeleteListCommand

        #region UiCommand
        var uiCommand = new Command("ui", "Browse and edit your tasks interactively. Also opens when todo is run with no arguments.");
        var uiListNameArgument = new Argument<string?>("listName", "List to open first.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        uiCommand.Add(uiListNameArgument);
        uiCommand.SetHandler(async (listName) =>
        {
            // Checked before signing in, so a piped "todo ui" fails fast instead of
            // prompting for a login it can't use.
            TodoTui.EnsureSupported();

            // Sign in (which may print a device-code prompt) before the UI takes over the screen.
            var actions = await todoActions.Value;
            TodoTui.Run(actions, listName);
        }, uiListNameArgument);
        rootCommand.Add(uiCommand);
        #endregion UiCommand

        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseExceptionHandler((exception, context) =>
            {
                // Errors the user can act on are reported as a plain message;
                // anything else keeps its stack trace for bug reports.
                context.Console.Error.Write(
                    (exception is TodoCliException ? exception.Message : exception.ToString()) + Environment.NewLine);
                context.ExitCode = 1;
            })
            .Build();

        return await parser.InvokeAsync(args);
    }

    #region ApiSetup
    static async Task<TodoActions> CreateTodoActionsAsync()
    {
        var authInformation = LoadAuthInformation();
        var authResult = await Auth.SignInSilently(authInformation);
        var graphClient = Auth.CreateGraphClient(authResult);
        return new TodoActions(new ApiQueries(graphClient));
    }

    static AzureADOauth LoadAuthInformation()
    {
        // Relative paths resolve against AppContext.BaseDirectory (next to the
        // executable), not the working directory, so the CLI works from anywhere.
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true);

        // The snap package ships its config outside the app directory.
        var configPathOverride = Environment.GetEnvironmentVariable("APP_CONFIG_PATH");
        if (!string.IsNullOrWhiteSpace(configPathOverride))
        {
            configBuilder.AddJsonFile(configPathOverride, optional: true);
        }

        var config = configBuilder.AddEnvironmentVariables("TODO_").Build();

        var authInformation = config.GetSection("AzureADInfo").Get<AzureADOauth>();
        if (authInformation is null
            || string.IsNullOrWhiteSpace(authInformation.ClientId)
            || authInformation.ClientId == ClientIdPlaceholder)
        {
            throw new TodoCliException(
                "No Azure AD client ID is configured, so todo cannot sign you in.\n" +
                $"Set AzureADInfo:ClientId in {Path.Combine(AppContext.BaseDirectory, "appsettings.json")}, " +
                "or set the TODO_AzureADInfo__ClientId environment variable.\n" +
                "The client ID comes from your Microsoft Entra app registration and is not a secret.");
        }

        return authInformation;
    }
    #endregion ApiSetup
}
