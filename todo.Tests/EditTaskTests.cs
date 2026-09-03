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
}
