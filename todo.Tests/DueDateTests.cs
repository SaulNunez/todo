namespace todo.Tests;

/// <summary>
/// Graph wants a time plus a timezone name. TimeZoneInfo.Local.StandardName only yields
/// a name Graph accepts where ICU supplies one, so dates go out as UTC instead.
/// </summary>
public class DueDateTests
{
    static StubGraph OneList()
    {
        var stub = new StubGraph();
        stub.Responder = request => request.Method == HttpMethod.Get
            ? """{"value":[{"id":"L1","displayName":"Shopping"}]}"""
            : """{"id":"T1"}""";
        return stub;
    }

    [Fact]
    public async Task Due_date_is_sent_as_utc()
    {
        var stub = OneList();
        var due = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Local);

        await stub.CreateTodoActions().CreateTask("Buy milk", "Shopping", dueDate: due);

        var body = stub.PostRequests.Single().Body;
        Assert.Contains("\"timeZone\":\"UTC\"", body);
        Assert.Contains(due.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss"), body);
    }

    [Fact]
    public async Task Reminder_date_is_sent_as_utc()
    {
        var stub = OneList();
        var reminder = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Local);

        await stub.CreateTodoActions().CreateTask("Buy milk", "Shopping", reminder: reminder);

        var body = stub.PostRequests.Single().Body;
        Assert.Contains("\"timeZone\":\"UTC\"", body);
        Assert.Contains(reminder.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss"), body);
    }

    [Fact]
    public async Task Creating_without_notes_sends_no_notes_body()
    {
        var stub = OneList();

        await stub.CreateTodoActions().CreateTask("Buy milk", "Shopping");

        Assert.DoesNotContain("\"body\"", stub.PostRequests.Single().Body);
    }

    [Fact]
    public async Task Creating_without_dates_sends_no_date_fields()
    {
        var stub = OneList();

        await stub.CreateTodoActions().CreateTask("Buy milk", "Shopping");

        var body = stub.PostRequests.Single().Body;
        Assert.DoesNotContain("dueDateTime", body);
        Assert.DoesNotContain("reminderDateTime", body);
    }
}
