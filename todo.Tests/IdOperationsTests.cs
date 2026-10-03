using TaskStatus = Microsoft.Graph.Models.TaskStatus;

namespace todo.Tests;

/// <summary>
/// The interactive UI acts on the IDs it already holds. Going through title lookup
/// would fail on duplicate titles and miss a task renamed a moment ago.
/// </summary>
public class IdOperationsTests
{
    static StubGraph Stub()
    {
        var stub = new StubGraph();
        stub.Responder = _ => """{"id":"T1","title":"Buy milk"}""";
        return stub;
    }

    [Fact]
    public async Task Editing_by_id_patches_the_task_without_any_lookups()
    {
        var stub = Stub();

        await stub.CreateTodoActions().EditTaskById("L1", "T1", status: TaskStatus.Completed);

        Assert.Empty(stub.GetRequests);
        Assert.EndsWith("/me/todo/lists/L1/tasks/T1", stub.PatchRequests.Single().Url);
    }

    [Fact]
    public async Task Deleting_by_id_deletes_the_task_without_any_lookups()
    {
        var stub = Stub();

        await stub.CreateTodoActions().DeleteTaskById("L1", "T1");

        var request = Assert.Single(stub.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.EndsWith("/me/todo/lists/L1/tasks/T1", request.Url);
    }

    [Fact]
    public async Task Creating_in_a_list_by_id_posts_without_looking_up_the_list()
    {
        var stub = Stub();

        await stub.CreateTodoActions().CreateTaskInList("L1", "Buy milk", notes: "Whole milk");

        Assert.Empty(stub.GetRequests);
        var post = stub.PostRequests.Single();
        Assert.EndsWith("/me/todo/lists/L1/tasks", post.Url);
        Assert.Contains("Buy milk", post.Body);
        Assert.Contains("Whole milk", post.Body);
    }

    [Fact]
    public async Task Due_dates_are_sent_in_utc()
    {
        var stub = Stub();
        var due = new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Local);

        await stub.CreateTodoActions().EditTaskById("L1", "T1", dueDate: due);

        var body = stub.PatchRequests.Single().Body;
        Assert.Contains(due.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss"), body);
        Assert.Contains("\"timeZone\":\"UTC\"", body);
    }
}
