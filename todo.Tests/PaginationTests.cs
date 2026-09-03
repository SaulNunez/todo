namespace todo.Tests;

/// <summary>
/// Graph returns one page at a time. Without following @odata.nextLink, tasks past the
/// first page are invisible to "todo tasks" and unfindable by check/uncheck/delete.
/// </summary>
public class PaginationTests
{
    [Fact]
    public async Task GetAvailableLists_follows_the_next_link()
    {
        var stub = new StubGraph();
        stub.RespondInSequence(
            """{"@odata.nextLink":"https://graph.microsoft.com/v1.0/me/todo/lists?$skiptoken=PAGE2","value":[{"id":"L1","displayName":"Work"}]}""",
            """{"value":[{"id":"L2","displayName":"Shopping"}]}""");

        var lists = await stub.CreateApiQueries().GetAvailableLists();

        Assert.Equal(new[] { "L1", "L2" }, lists.Select(l => l.Id));
        Assert.Equal(2, stub.GetRequests.Count());
        Assert.Contains("skiptoken=PAGE2", stub.Requests[1].Url);
    }

    [Fact]
    public async Task GetTasksInList_follows_the_next_link()
    {
        var stub = new StubGraph();
        stub.RespondInSequence(
            """{"@odata.nextLink":"https://graph.microsoft.com/v1.0/me/todo/lists/L1/tasks?$skiptoken=PAGE2","value":[{"id":"T1","title":"Buy milk"}]}""",
            """{"value":[{"id":"T2","title":"Buy eggs"}]}""");

        var tasks = await stub.CreateApiQueries().GetTasksInList("L1");

        Assert.Equal(new[] { "Buy milk", "Buy eggs" }, tasks.Select(t => t.Title));
    }

    [Fact]
    public async Task A_task_on_the_second_page_is_still_findable()
    {
        // This is the user-visible symptom: check/delete reporting "couldn't be found"
        // for a task that exists.
        var stub = new StubGraph();
        stub.RespondInSequence(
            """{"@odata.nextLink":"https://graph.microsoft.com/v1.0/me/todo/lists/L1/tasks?$skiptoken=PAGE2","value":[{"id":"T1","title":"Buy milk"}]}""",
            """{"value":[{"id":"T2","title":"Buy eggs"}]}""");

        Assert.Equal("T2", await stub.CreateApiQueries().GetTaskId("Buy eggs", "L1"));
    }

    [Fact]
    public async Task A_single_page_response_issues_one_request()
    {
        var stub = new StubGraph();
        stub.Responder = _ => """{"value":[{"id":"L1","displayName":"Work"}]}""";

        await stub.CreateApiQueries().GetAvailableLists();

        Assert.Single(stub.Requests);
    }
}
