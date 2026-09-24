using WorkOrderDesk.Intake;

namespace WorkOrderDesk.Tests;

public class SuggestionValidatorTests
{
    private readonly SuggestionValidator _validator = new();

    [Fact]
    public void AcceptsKnownLabels()
    {
        var json = """{"category":"plumbing","urgency":"urgent","reply":"We will send someone.","reason":"Active leak."}""";

        Assert.True(_validator.TryReadJson(json, out var suggestion));
        Assert.Equal("plumbing", suggestion!.Category);
        Assert.Equal("urgent", suggestion.Urgency);
        Assert.Equal("model", suggestion.Source);
    }

    [Fact]
    public void RejectsUnknownCategory()
    {
        var json = """{"category":"carpentry","urgency":"low","reply":"Ok.","reason":"Guess."}""";

        Assert.False(_validator.TryReadJson(json, out _));
    }

    [Fact]
    public void RejectsMissingReply()
    {
        var json = """{"category":"other","urgency":"low","reply":"","reason":"Not sure."}""";

        Assert.False(_validator.TryReadJson(json, out _));
    }

    [Fact]
    public void StripsMarkdownFence()
    {
        var json = "```json\n{\"category\":\"hvac\",\"urgency\":\"urgent\",\"reply\":\"Heat is out. We are on it.\",\"reason\":\"No heat overnight.\"}\n```";

        Assert.True(_validator.TryReadJson(json, out var suggestion));
        Assert.Equal("hvac", suggestion!.Category);
    }
}
