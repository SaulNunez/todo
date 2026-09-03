namespace todo.Tests;

/// <summary>
/// Covers <see cref="ApiQueries.GetListId"/>. A wrong answer here is silent: the command
/// succeeds against a list the user did not name.
/// </summary>
public class ListMatchingTests
{
    const string ThreeLists = """
        {"value":[
            {"id":"L1","displayName":"Homework"},
            {"id":"L2","displayName":"Work"},
            {"id":"L3","displayName":"🛒 Shopping"}
        ]}
        """;

    static StubGraph Lists()
    {
        var stub = new StubGraph();
        stub.Responder = _ => ThreeLists;
        return stub;
    }

    [Fact]
    public async Task Exact_title_wins_over_a_substring_match()
    {
        // "Work" is a substring of "Homework", which is returned first.
        Assert.Equal("L2", await Lists().CreateApiQueries().GetListId("Work"));
    }

    [Theory]
    [InlineData("work")]
    [InlineData("WORK")]
    [InlineData("  Work  ")]
    public async Task Exact_match_ignores_case_and_surrounding_space(string name)
    {
        Assert.Equal("L2", await Lists().CreateApiQueries().GetListId(name));
    }

    [Fact]
    public async Task Substring_still_matches_emoji_decorated_titles()
    {
        Assert.Equal("L3", await Lists().CreateApiQueries().GetListId("Shopping"));
    }

    [Fact]
    public async Task Unknown_list_returns_null()
    {
        Assert.Null(await Lists().CreateApiQueries().GetListId("Nope"));
    }

    [Fact]
    public async Task Ambiguous_substring_throws_and_names_the_candidates()
    {
        // "ork" is inside both "Homework" and "Work".
        var exception = await Assert.ThrowsAsync<TodoCliException>(
            () => Lists().CreateApiQueries().GetListId("ork"));

        Assert.Contains("matches 2 lists", exception.Message);
        Assert.Contains("Homework", exception.Message);
        Assert.Contains("Work", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_name_is_rejected_before_any_api_call(string blank)
    {
        // A blank name used to match every list, so the command hit an arbitrary one.
        var stub = Lists();

        await Assert.ThrowsAsync<TodoCliException>(() => stub.CreateApiQueries().GetListId(blank));

        Assert.Empty(stub.Requests);
    }
}
