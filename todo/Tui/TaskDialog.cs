using System.Globalization;
using Microsoft.Graph.Models;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TaskStatus = Microsoft.Graph.Models.TaskStatus;

namespace todo.Tui;

/// <summary>
/// What the user changed in <see cref="TaskDialog"/>. Every field is null when untouched,
/// so an edit turns into a PATCH of only those fields.
/// </summary>
public record TaskEdit(
    string? Title,
    string? Notes,
    DateTime? DueDate,
    DateTime? Reminder,
    TaskStatus? Status,
    bool ClearDueDate,
    bool ClearReminder)
{
    public bool IsEmpty => Title is null && Notes is null && DueDate is null && Reminder is null
        && Status is null && !ClearDueDate && !ClearReminder;
}

/// <summary>
/// Create/edit form for a task. Dates are plain text fields so that blank can mean
/// "no date", parsed the same way as the CLI's --due-date and --reminder-date.
/// </summary>
public class TaskDialog : Dialog<TaskEdit>
{
    private readonly TodoTask? original;
    private readonly TextField titleField;
    private readonly TextField dueField;
    private readonly TextField reminderField;
    // TextView is marked obsolete in favour of tui-cs/Editor, which has no stable release
    // yet. It is still the only multi-line input in Terminal.Gui itself.
#pragma warning disable CS0618
    private readonly TextView notesField;
#pragma warning restore CS0618
    private readonly CheckBox? completedBox;
    private readonly Label errorLabel;

    public TaskDialog(TodoTask? existing)
    {
        original = existing;
        Title = existing is null ? "New task" : "Edit task";

        var titleLabel = new Label { Text = "_Title:", X = 0, Y = 0 };
        titleField = new TextField
        {
            Text = existing?.Title ?? "",
            X = 12, Y = 0, Width = 50
        };

        var dueLabel = new Label { Text = "_Due date:", X = 0, Y = 2 };
        dueField = new TextField
        {
            Text = FormatDate(existing?.DueDateTime),
            X = 12, Y = 2, Width = 20
        };
        var dueHint = new Label { Text = "e.g. 2026-10-31, blank for none", X = 34, Y = 2 };

        var reminderLabel = new Label { Text = "_Reminder:", X = 0, Y = 3 };
        reminderField = new TextField
        {
            Text = FormatDateTime(existing?.ReminderDateTime),
            X = 12, Y = 3, Width = 20
        };
        var reminderHint = new Label { Text = "e.g. 2026-10-30 09:00", X = 34, Y = 3 };

        var notesLabel = new Label { Text = "_Notes:", X = 0, Y = 5 };
#pragma warning disable CS0618
        notesField = new TextView
        {
            Text = existing?.Body?.Content ?? "",
            X = 12, Y = 5, Width = 50, Height = 6,
            WordWrap = true
        };
#pragma warning restore CS0618

        Add(titleLabel, titleField, dueLabel, dueField, dueHint, reminderLabel, reminderField, reminderHint,
            notesLabel, notesField);

        var nextRow = 12;
        if (existing is not null)
        {
            completedBox = new CheckBox
            {
                Text = "_Completed",
                X = 12, Y = nextRow,
                Value = existing.Status == TaskStatus.Completed ? CheckState.Checked : CheckState.UnChecked
            };
            Add(completedBox);
            nextRow++;
        }

        errorLabel = new Label { Text = "", X = 0, Y = nextRow, Width = Dim.Fill() };
        Add(errorLabel);

        AddButton(new Button { Text = "_Cancel" });
        AddButton(new Button { Text = "_Save" });
    }

    protected override bool OnAccepting(CommandEventArgs args)
    {
        // Cancel is a non-default button: let the base class close the dialog with no Result.
        View? source = null;
        args.Context?.Source?.TryGetTarget(out source);
        if (source is Button { IsDefault: false })
        {
            return base.OnAccepting(args);
        }

        if (!TryBuildEdit(out var edit, out var error))
        {
            errorLabel.Text = error;
            return true; // handled: keep the dialog open so the user can fix it
        }

        Result = edit;
        return base.OnAccepting(args);
    }

    private bool TryBuildEdit(out TaskEdit edit, out string error)
    {
        edit = new TaskEdit(null, null, null, null, null, false, false);
        error = "";

        var title = titleField.Text.Trim();
        if (title.Length == 0)
        {
            error = "A title is required.";
            return false;
        }

        if (!TryParseOptionalDate(dueField.Text, out var due))
        {
            error = $"\"{dueField.Text}\" isn't a date.";
            return false;
        }

        if (!TryParseOptionalDate(reminderField.Text, out var reminder))
        {
            error = $"\"{reminderField.Text}\" isn't a date and time.";
            return false;
        }

        var notes = notesField.Text;

        if (original is null)
        {
            edit = new TaskEdit(title, notes.Length == 0 ? null : notes, due, reminder, null, false, false);
            return true;
        }

        // Only report fields that differ from what was loaded, so that untouched fields
        // are never written back.
        var dueChanged = dueField.Text.Trim() != FormatDate(original.DueDateTime);
        var reminderChanged = reminderField.Text.Trim() != FormatDateTime(original.ReminderDateTime);
        var originalNotes = original.Body?.Content ?? "";

        TaskStatus? status = null;
        if (completedBox is not null)
        {
            var wasCompleted = original.Status == TaskStatus.Completed;
            var isCompleted = completedBox.Value == CheckState.Checked;
            if (wasCompleted != isCompleted)
            {
                status = isCompleted ? TaskStatus.Completed : TaskStatus.NotStarted;
            }
        }

        edit = new TaskEdit(
            Title: title != original.Title ? title : null,
            Notes: notes != originalNotes ? notes : null,
            DueDate: dueChanged ? due : null,
            Reminder: reminderChanged ? reminder : null,
            Status: status,
            ClearDueDate: dueChanged && due is null,
            ClearReminder: reminderChanged && reminder is null);
        return true;
    }

    private static bool TryParseOptionalDate(string text, out DateTime? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (DateTime.TryParse(text.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    internal static string FormatDate(DateTimeTimeZone? value) =>
        ToLocal(value)?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    internal static string FormatDateTime(DateTimeTimeZone? value) =>
        ToLocal(value)?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "";

    /// <summary>
    /// The SDK's ToDateTimeOffset() only accepts Graph's usual seven fractional digits
    /// and throws on anything else. One odd value shouldn't take the whole UI down, so
    /// fall back to a lenient parse for UTC values and show nothing otherwise.
    /// </summary>
    internal static DateTime? ToLocal(DateTimeTimeZone? value)
    {
        if (value?.DateTime is null)
        {
            return null;
        }

        try
        {
            return value.ToDateTimeOffset().LocalDateTime;
        }
        catch (Exception e) when (e is FormatException or TimeZoneNotFoundException)
        {
            if (value.TimeZone == "UTC" && DateTime.TryParse(value.DateTime, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var utc))
            {
                return utc.ToLocalTime();
            }

            return null;
        }
    }
}
