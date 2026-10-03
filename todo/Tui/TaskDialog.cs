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
/// Create/edit form for a task. Dates use Terminal.Gui's DateEditor/TimeEditor. Those
/// always hold a value, so a checkbox next to each one says whether the task has that
/// date at all.
/// </summary>
public class TaskDialog : Dialog<TaskEdit>
{
    private const int FieldColumn = 14;

    private readonly TodoTask? original;
    private readonly TextField titleField;
    private readonly CheckBox hasDueBox;
    private readonly DateEditor dueEditor;
    private readonly CheckBox hasReminderBox;
    private readonly DateEditor reminderDateEditor;
    private readonly TimeEditor reminderTimeEditor;
    // TextView is marked obsolete in favour of tui-cs/Editor, which has no stable release
    // yet. It is still the only multi-line input in Terminal.Gui itself.
#pragma warning disable CS0618
    private readonly TextView notesField;
#pragma warning restore CS0618
    private readonly CheckBox? completedBox;
    private readonly Label errorLabel;

    private readonly DateTime? originalDue;
    private readonly DateTime? originalReminder;

    public TaskDialog(TodoTask? existing)
    {
        original = existing;
        Title = existing is null ? "New task" : "Edit task";

        originalDue = ToLocal(existing?.DueDateTime)?.Date;
        originalReminder = TruncateToMinute(ToLocal(existing?.ReminderDateTime));

        var titleLabel = new Label { Text = "_Title:", X = 0, Y = 0 };
        titleField = new TextField
        {
            Text = existing?.Title ?? "",
            X = FieldColumn, Y = 0, Width = 50
        };

        hasDueBox = new CheckBox { Text = "_Due date", X = 0, Y = 2, Value = Checked(originalDue is not null) };
        dueEditor = new DateEditor
        {
            X = FieldColumn, Y = 2,
            Value = originalDue ?? DateTime.Today
        };

        // A new reminder defaults to 09:00 on the due date, or today without one.
        var reminderDefault = originalReminder ?? (originalDue ?? DateTime.Today).AddHours(9);
        hasReminderBox = new CheckBox { Text = "_Reminder", X = 0, Y = 3, Value = Checked(originalReminder is not null) };
        reminderDateEditor = new DateEditor
        {
            X = FieldColumn, Y = 3,
            Value = reminderDefault.Date
        };
        reminderTimeEditor = new TimeEditor
        {
            X = Pos.Right(reminderDateEditor) + 1, Y = 3,
            Format = ShortTimeFormat(),
            Value = reminderDefault.TimeOfDay
        };

        var notesLabel = new Label { Text = "_Notes:", X = 0, Y = 5 };
#pragma warning disable CS0618
        notesField = new TextView
        {
            Text = existing?.Body?.Content ?? "",
            X = FieldColumn, Y = 5, Width = 50, Height = 6,
            WordWrap = true
        };
#pragma warning restore CS0618

        Add(titleLabel, titleField, hasDueBox, dueEditor, hasReminderBox, reminderDateEditor, reminderTimeEditor,
            notesLabel, notesField);

        // Editors for a date the task doesn't have are greyed out and skipped by Tab.
        hasDueBox.ValueChanged += (_, _) => UpdateEnabled();
        hasReminderBox.ValueChanged += (_, _) => UpdateEnabled();
        UpdateEnabled();

        var nextRow = 12;
        if (existing is not null)
        {
            completedBox = new CheckBox
            {
                Text = "_Completed",
                X = FieldColumn, Y = nextRow,
                Value = Checked(existing.Status == TaskStatus.Completed)
            };
            Add(completedBox);
            nextRow++;
        }

        errorLabel = new Label { Text = "", X = 0, Y = nextRow, Width = Dim.Fill() };
        Add(errorLabel);

        AddButton(new Button { Text = "_Cancel" });
        AddButton(new Button { Text = "_Save" });
    }

    private static CheckState Checked(bool value) => value ? CheckState.Checked : CheckState.UnChecked;

    private static DateTime? TruncateToMinute(DateTime? value) =>
        value is { } v ? new DateTime(v.Year, v.Month, v.Day, v.Hour, v.Minute, 0, v.Kind) : null;

    /// <summary>TimeEditor shows the long time pattern by default; reminders don't need seconds.</summary>
    private static DateTimeFormatInfo ShortTimeFormat()
    {
        var format = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone();
        format.LongTimePattern = format.ShortTimePattern;
        return format;
    }

    private void UpdateEnabled()
    {
        dueEditor.Enabled = hasDueBox.Value == CheckState.Checked;
        reminderDateEditor.Enabled = reminderTimeEditor.Enabled = hasReminderBox.Value == CheckState.Checked;
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

        DateTime? due = hasDueBox.Value == CheckState.Checked ? dueEditor.Value.Date : null;
        DateTime? reminder = hasReminderBox.Value == CheckState.Checked
            ? TruncateToMinute(reminderDateEditor.Value.Date + reminderTimeEditor.Value)
            : null;
        var notes = notesField.Text;

        if (original is null)
        {
            edit = new TaskEdit(title, notes.Length == 0 ? null : notes, due, reminder, null, false, false);
            return true;
        }

        // Only report fields that differ from what was loaded, so that untouched fields
        // are never written back.
        var dueChanged = due != originalDue;
        var reminderChanged = reminder != originalReminder;
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
