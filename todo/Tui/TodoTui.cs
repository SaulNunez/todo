using System.Collections.ObjectModel;
using Microsoft.Graph.Models;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TaskStatus = Microsoft.Graph.Models.TaskStatus;
using TuiAttribute = Terminal.Gui.Drawing.Attribute;

namespace todo.Tui;

/// <summary>
/// Full-screen interactive mode: lists on the left, the selected list's tasks on the
/// right, and the selected task's details underneath.
/// </summary>
public static class TodoTui
{
    /// <summary>
    /// A TUI needs a real terminal on both ends. Piped or redirected runs (scripts, cron)
    /// keep the plain CLI behaviour.
    /// </summary>
    public static bool IsSupported =>
        !Console.IsInputRedirected
        && !Console.IsOutputRedirected
        && Environment.GetEnvironmentVariable("TERM") != "dumb";

    public static void EnsureSupported()
    {
        if (!IsSupported)
        {
            throw new TodoCliException("The interactive UI needs an interactive terminal.");
        }
    }

    public static void Run(TodoActions actions, string? initialList)
    {
        EnsureSupported();

        using IApplication app = Terminal.Gui.App.Application.Create();
        app.Init();

        using var window = new TodoWindow(app, actions, initialList);
        app.Run(window);
    }
}

internal sealed class TodoWindow : Window
{
    private readonly IApplication app;
    private readonly TodoActions actions;
    private readonly string? initialList;

    private readonly ListView listsView;
    private readonly ListView tasksView;
    private readonly Label detailsView;
    private readonly FrameView tasksFrame;
    private readonly Shortcut statusText;

    private readonly ObservableCollection<string> listNames = [];
    private readonly ObservableCollection<string> taskRows = [];
    private List<TodoTaskList> lists = [];
    private List<TodoTask> tasks = [];

    private string? currentListId;
    private int pendingOperations;

    public TodoWindow(IApplication app, TodoActions actions, string? initialList)
    {
        this.app = app;
        this.actions = actions;
        this.initialList = initialList;

        Title = "todo";
        BorderStyle = LineStyle.None;

        var listsFrame = new FrameView
        {
            Title = "_Lists",
            X = 0, Y = 0,
            Width = Dim.Percent(30),
            Height = Dim.Fill(1),
            // FrameViews are tab groups by default, which would make Tab cycle inside
            // one pane only. Plain tab stops let Tab move between lists and tasks.
            TabStop = TabBehavior.TabStop
        };
        listsView = new ListView { Width = Dim.Fill(), Height = Dim.Fill() };
        listsView.SetSource(listNames);
        listsView.ValueChanged += (_, _) => OnListSelected();
        listsFrame.Add(listsView);

        tasksFrame = new FrameView
        {
            Title = "_Tasks",
            X = Pos.Right(listsFrame), Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(Dim.Func(_ => DetailsHeight + 1)),
            TabStop = TabBehavior.TabStop
        };
        tasksView = new ListView { Width = Dim.Fill(), Height = Dim.Fill() };
        tasksView.SetSource(taskRows);
        tasksView.ValueChanged += (_, _) => ShowDetails();
        tasksView.RowRender += OnTaskRowRender;
        tasksView.KeyDown += OnTasksKeyDown;
        tasksView.Accepting += (_, e) =>
        {
            EditSelectedTask();
            e.Handled = true;
        };
        tasksFrame.Add(tasksView);

        // ListView binds Ctrl+N to "move down" (emacs style), which would swallow the
        // New shortcut whenever a list has focus.
        listsView.KeyBindings.Remove(Key.N.WithCtrl);
        tasksView.KeyBindings.Remove(Key.N.WithCtrl);

        var detailsFrame = new FrameView
        {
            Title = "Details",
            X = Pos.Right(listsFrame), Y = Pos.Bottom(tasksFrame),
            Width = Dim.Fill(),
            Height = DetailsHeight
        };
        detailsView = new Label { Width = Dim.Fill(), Height = Dim.Fill() };
        detailsView.TextFormatter.WordWrap = true;
        detailsFrame.Add(detailsView);

        statusText = new Shortcut { Title = "", CanFocus = false };
        var statusBar = new StatusBar(
        [
            new Shortcut(Key.N.WithCtrl, "New", NewTask),
            new Shortcut(Key.F2, "Edit", EditSelectedTask),
            new Shortcut(Key.Space, "Done/Undone", ToggleSelectedTask),
            new Shortcut(Key.Delete, "Delete", DeleteSelectedTask),
            new Shortcut(Key.F5, "Refresh", Refresh),
            new Shortcut(Key.Q.WithCtrl, "Quit", () => app.RequestStop()),
            statusText
        ]);

        Add(listsFrame, tasksFrame, detailsFrame, statusBar);

        Initialized += (_, _) => LoadLists();
    }

    private const int DetailsHeight = 9;

    private TodoTask? SelectedTask =>
        tasksView.SelectedItem is int index && index >= 0 && index < tasks.Count ? tasks[index] : null;

    #region Loading

    private void LoadLists(string? selectListId = null)
    {
        RunInBackground("Loading lists…", () => actions.GetAllLists(), loaded =>
        {
            lists = loaded;
            listNames.Clear();
            foreach (var list in lists)
            {
                listNames.Add(list.DisplayName ?? "(untitled)");
            }

            if (lists.Count == 0)
            {
                ShowDetails();
                return;
            }

            var firstLoad = currentListId is null;
            if (firstLoad && initialList is not null)
            {
                // Show the first list meanwhile, so an unknown name still leaves
                // something usable behind the error.
                SelectList(lists[0].Id);

                // Same matching rules as "todo tasks <name>", including emoji-tolerant
                // partial matches and the ambiguity error.
                RunInBackground("Finding list…", () => actions.ResolveListId(initialList), id =>
                {
                    SelectList(id);
                    tasksView.SetFocus();
                });
                return;
            }

            SelectList(selectListId ?? currentListId ?? lists[0].Id);
            if (firstLoad)
            {
                tasksView.SetFocus();
            }
        });
    }

    private void SelectList(string? listId)
    {
        var index = lists.FindIndex(l => l.Id == listId);
        if (index < 0)
        {
            index = 0;
        }

        if (listsView.SelectedItem == index)
        {
            // ValueChanged won't fire, so load explicitly.
            OnListSelected();
        }
        else
        {
            listsView.SelectedItem = index;
        }
    }

    private void OnListSelected()
    {
        if (listsView.SelectedItem is not int index || index < 0 || index >= lists.Count)
        {
            return;
        }

        var list = lists[index];
        currentListId = list.Id;
        tasksFrame.Title = $"_Tasks: {list.DisplayName}";
        LoadTasks();
    }

    private void LoadTasks(string? selectTaskId = null)
    {
        var listId = currentListId;
        if (listId is null)
        {
            return;
        }

        RunInBackground("Loading tasks…", () => actions.GetTasks(listId), loaded =>
        {
            // The user may have moved to another list while this was loading.
            if (listId != currentListId)
            {
                return;
            }

            // Open tasks first, then completed ones, like the official apps.
            tasks = loaded
                .OrderBy(t => t.Status == TaskStatus.Completed)
                .ToList();

            taskRows.Clear();
            foreach (var task in tasks)
            {
                taskRows.Add(FormatRow(task));
            }

            var selected = selectTaskId is null ? -1 : tasks.FindIndex(t => t.Id == selectTaskId);
            tasksView.SelectedItem = tasks.Count == 0 ? null : Math.Max(selected, 0);
            ShowDetails();
        });
    }

    private void Refresh() => LoadLists(currentListId);

    #endregion Loading

    #region Rendering

    private static bool IsOverdue(TodoTask task) =>
        task.Status != TaskStatus.Completed
        && TaskDialog.ToLocal(task.DueDateTime) is { } due
        && due < DateTime.Now;

    private static string FormatRow(TodoTask task)
    {
        var check = task.Status == TaskStatus.Completed ? "[x]" : "[ ]";
        var due = task.DueDateTime is null ? "" : $"  due {TaskDialog.FormatDate(task.DueDateTime)}";
        var overdue = IsOverdue(task) ? " !" : "";
        return $"{check} {task.Title}{due}{overdue}";
    }

    private void OnTaskRowRender(object? sender, ListViewRowEventArgs e)
    {
        if (e.Row < 0 || e.Row >= tasks.Count || e.Row == tasksView.SelectedItem)
        {
            return;
        }

        var task = tasks[e.Row];
        var normal = tasksView.GetAttributeForRole(VisualRole.Normal);
        if (IsOverdue(task))
        {
            e.RowAttribute = new TuiAttribute(Color.Red, normal.Background);
        }
        else if (task.Status == TaskStatus.Completed)
        {
            e.RowAttribute = new TuiAttribute(Color.DarkGray, normal.Background);
        }
    }

    private void ShowDetails()
    {
        var task = SelectedTask;
        if (task is null)
        {
            detailsView.Text = lists.Count == 0
                ? "No lists yet. Create one with: todo lists add \"My list\""
                : tasks.Count == 0 ? "This list is empty. Press Ctrl+N to add a task." : "";
            return;
        }

        var lines = new List<string>
        {
            task.Title ?? "",
            $"Status:   {task.Status}"
        };
        if (task.DueDateTime is not null)
        {
            lines.Add($"Due:      {TaskDialog.FormatDate(task.DueDateTime)}");
        }
        if (task.ReminderDateTime is not null)
        {
            lines.Add($"Reminder: {TaskDialog.FormatDateTime(task.ReminderDateTime)}");
        }
        if (!string.IsNullOrWhiteSpace(task.Body?.Content))
        {
            lines.Add("");
            lines.Add(task.Body.Content);
        }

        detailsView.Text = string.Join("\n", lines);
    }

    #endregion Rendering

    #region Task actions

    private void OnTasksKeyDown(object? sender, Key key)
    {
        // Handled here rather than only through the status bar, because ListView binds
        // Space and Delete itself and would otherwise swallow them.
        if (key == Key.Space)
        {
            ToggleSelectedTask();
            key.Handled = true;
        }
        else if (key == Key.Delete || key == Key.Backspace)
        {
            DeleteSelectedTask();
            key.Handled = true;
        }
    }

    private void NewTask()
    {
        var listId = currentListId;
        if (listId is null)
        {
            return;
        }

        using var dialog = new TaskDialog(null);
        app.Run(dialog);
        if (dialog.Result is not { } edit)
        {
            return;
        }

        RunInBackground("Creating task…",
            () => actions.CreateTaskInList(listId, edit.Title!, edit.DueDate, edit.Reminder, edit.Notes),
            created => LoadTasks(created?.Id));
    }

    private void EditSelectedTask()
    {
        var listId = currentListId;
        if (SelectedTask is not { Id: { } taskId } task || listId is null)
        {
            return;
        }

        using var dialog = new TaskDialog(task);
        app.Run(dialog);
        if (dialog.Result is not { IsEmpty: false } edit)
        {
            return;
        }

        RunInBackground("Saving task…",
            () => actions.EditTaskById(listId, taskId, edit.Title, edit.Status, edit.DueDate, edit.Reminder,
                edit.Notes, edit.ClearDueDate, edit.ClearReminder),
            _ => LoadTasks(taskId));
    }

    private void ToggleSelectedTask()
    {
        var listId = currentListId;
        if (SelectedTask is not { Id: { } taskId } task || listId is null)
        {
            return;
        }

        var status = task.Status == TaskStatus.Completed ? TaskStatus.NotStarted : TaskStatus.Completed;
        RunInBackground("Saving task…",
            () => actions.EditTaskById(listId, taskId, status: status),
            _ => LoadTasks(taskId));
    }

    private void DeleteSelectedTask()
    {
        var listId = currentListId;
        if (SelectedTask is not { Id: { } taskId } task || listId is null)
        {
            return;
        }

        // The last button is the default, so Cancel goes last: a stray Enter keeps the task.
        var answer = MessageBox.Query(app, "Delete task", $"Delete \"{task.Title}\"?", "_Delete", "_Cancel");
        if (answer != 0)
        {
            return;
        }

        // Keep the cursor near where it was: select the task that follows the deleted one.
        var index = tasks.IndexOf(task);
        var neighbourId = index + 1 < tasks.Count ? tasks[index + 1].Id
            : index > 0 ? tasks[index - 1].Id
            : null;

        RunInBackground("Deleting task…",
            async () => { await actions.DeleteTaskById(listId, taskId); return true; },
            _ => LoadTasks(neighbourId));
    }

    #endregion Task actions

    /// <summary>
    /// Runs a Graph call off the UI loop, then applies the result back on it. Errors are
    /// shown in a message box instead of tearing down the UI.
    /// </summary>
    private void RunInBackground<T>(string busyText, Func<Task<T>> work, Action<T> onDone)
    {
        pendingOperations++;
        statusText.Title = busyText;

        _ = Task.Run(async () =>
        {
            try
            {
                var result = await work().ConfigureAwait(false);
                app.Invoke(() =>
                {
                    Finish();
                    try
                    {
                        onDone(result);
                    }
                    catch (Exception exception)
                    {
                        ShowError(exception);
                    }
                });
            }
            catch (Exception exception)
            {
                app.Invoke(() =>
                {
                    Finish();
                    ShowError(exception);
                });
            }
        });

        void Finish()
        {
            pendingOperations--;
            if (pendingOperations == 0)
            {
                statusText.Title = "";
            }
        }

        void ShowError(Exception exception)
        {
            var message = exception is TodoCliException ? exception.Message : exception.ToString();
            MessageBox.ErrorQuery(app, "Error", message, "_OK");
        }
    }
}
