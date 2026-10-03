using Microsoft.Graph.Models;
using TaskStatus = Microsoft.Graph.Models.TaskStatus;

namespace todo.Tests;

/// <summary>
/// Edits are sent as a minimal PATCH. Anything extra in the body is a field the server
/// will overwrite - which is how check/uncheck used to erase a task's notes.
/// </summary>
public class EditTaskTests
{
    static StubGraph OneListOneTask()
    {
        var stub = new StubGraph();
        stub.Responder = request =>
        {
            if (request.Method != HttpMethod.Get)
            {
                return """{"id":"T1","title":"Buy milk"}""";
            }

            return request.RequestUri!.ToString().Contains("/tasks")
                ? """{"value":[{"id":"T1","title":"Buy milk"}]}"""
                : """{"value":[{"id":"L1","displayName":"Shopping"}]}""";
        };
        return stub;
    }

    [Fact]
    public async Task Editing_does_not_read_the_task_first()
    {
        var stub = new StubGraph();
        stub.Responder = _ => """{"id":"T1"}""";

        await stub.CreateApiQueries().EditTask("T1", "L1", status: TaskStatus.Completed);

        Assert.Empty(stub.GetRequests);
        Assert.Single(stub.PatchRequests);
    }

    [Fact]
    public async Task Patch_contains_the_changed_field()
    {
        var stub = new StubGraph();
        stub.Responder = _ => """{"id":"T1"}""";

        await stub.CreateApiQueries().EditTask("T1", "L1", status: TaskStatus.Completed);

        Assert.Contains("completed", stub.PatchRequests.Single().Body);
    }

    [Fact]
    public async Task Patch_omits_server_owned_fields()
    {
        var stub = new StubGraph();
        stub.Responder = _ => """{"id":"T1"}""";

        await stub.CreateApiQueries().EditTask("T1", "L1", status: TaskStatus.Completed);

        var body = stub.PatchRequests.Single().Body;
        Assert.DoesNotContain("createdDateTime", body);
        Assert.DoesNotContain("lastModifiedDateTime", body);
        Assert.DoesNotContain("\"id\"", body);
    }

    [Theory]
    [InlineData(TaskStatus.Completed)]
    [InlineData(TaskStatus.NotStarted)]
    public async Task Check_and_uncheck_do_not_erase_notes(TaskStatus status)
    {
        // Regression: notes defaulted to "" rather than null, so every check/uncheck
        // sent body:{content:""} and wiped whatever the user had written.
        var stub = OneListOneTask();

        await stub.CreateTodoActions().EditTask("Buy milk", "Shopping", status: status);

        Assert.DoesNotContain("\"body\"", stub.PatchRequests.Single().Body);
    }

    [Fact]
    public async Task Notes_are_sent_when_the_caller_supplies_them()
    {
        // Negative control: proves the assertion above is sensitive to the body field
        // rather than passing because the request is empty.
        var stub = new StubGraph();
        stub.Responder = _ => """{"id":"T1"}""";

        await stub.CreateApiQueries().EditTask("T1", "L1", notes: "Whole milk");

        var body = stub.PatchRequests.Single().Body;
        Assert.Contains("\"body\"", body);
        Assert.Contains("Whole milk", body);
    }

    [Fact]
    public async Task Missing_list_reports_the_name_the_user_typed()
    {
        var stub = new StubGraph();
        stub.Responder = _ => """{"value":[]}""";

        var exception = await Assert.ThrowsAsync<TodoCliException>(
            () => stub.CreateTodoActions().EditTask("Buy milk", "Nope", status: TaskStatus.Completed));

        Assert.Contains("Nope", exception.Message);
    }

    [Fact]
    public async Task Clearing_the_due_date_sends_an_explicit_null()
    {
        // A null dueDate argument means "leave alone", so removing a date from the
        // interactive editor needs the flag to put dueDateTime:null on the wire.
        var stub = new StubGraph();
        stub.Responder = _ => """{"id":"T1"}""";

        await stub.CreateApiQueries().EditTask("T1", "L1", clearDueDate: true);

        var body = stub.PatchRequests.Single().Body;
        Assert.Contains("\"dueDateTime\":null", body);
        // Regression: assigning null to the typed property made Kiota write the key twice.
        Assert.Equal(1, Occurrences(body, "\"dueDateTime\""));
    }

    static int Occurrences(string text, string value) =>
        (text.Length - text.Replace(value, "").Length) / value.Length;

    [Fact]
    public async Task Clearing_the_reminder_sends_an_explicit_null_and_turns_it_off()
    {
        var stub = new StubGraph();
        stub.Responder = _ => """{"id":"T1"}""";

        await stub.CreateApiQueries().EditTask("T1", "L1", clearReminder: true);

        var body = stub.PatchRequests.Single().Body;
        Assert.Contains("\"reminderDateTime\":null", body);
        Assert.Equal(1, Occurrences(body, "\"reminderDateTime\""));
        Assert.Contains("\"isReminderOn\":false", body);
    }

    [Fact]
    public async Task Edits_without_the_clear_flags_leave_dates_out()
    {
        // Negative control for the two tests above.
        var stub = new StubGraph();
        stub.Responder = _ => """{"id":"T1"}""";

        await stub.CreateApiQueries().EditTask("T1", "L1", status: TaskStatus.Completed);

        var body = stub.PatchRequests.Single().Body;
        Assert.DoesNotContain("dueDateTime", body);
        Assert.DoesNotContain("reminderDateTime", body);
        Assert.DoesNotContain("isReminderOn", body);
    }

    [Fact]
    public async Task Setting_a_reminder_turns_it_on()
    {
        // Without isReminderOn, To Do stores the reminder time but never sends the alert.
        var stub = new StubGraph();
        stub.Responder = _ => """{"id":"T1"}""";
        var reminder = new DateTimeTimeZone { DateTime = "2026-10-30T15:00:00", TimeZone = "UTC" };

        await stub.CreateApiQueries().EditTask("T1", "L1", reminder: reminder);

        var body = stub.PatchRequests.Single().Body;
        Assert.Contains("\"reminderDateTime\"", body);
        Assert.Contains("\"isReminderOn\":true", body);
    }

    [Fact]
    public async Task Creating_a_task_with_a_reminder_turns_it_on()
    {
        var stub = new StubGraph();
        stub.Responder = _ => """{"id":"T1"}""";
        var reminder = new DateTimeTimeZone { DateTime = "2026-10-30T15:00:00", TimeZone = "UTC" };

        await stub.CreateApiQueries().CreateTask("Buy milk", "L1", reminder: reminder);

        Assert.Contains("\"isReminderOn\":true", stub.PostRequests.Single().Body);
    }

    [Fact]
    public async Task Creating_a_task_without_a_reminder_leaves_it_off()
    {
        // Negative control for the test above.
        var stub = new StubGraph();
        stub.Responder = _ => """{"id":"T1"}""";

        await stub.CreateApiQueries().CreateTask("Buy milk", "L1");

        Assert.DoesNotContain("isReminderOn", stub.PostRequests.Single().Body);
    }
}
