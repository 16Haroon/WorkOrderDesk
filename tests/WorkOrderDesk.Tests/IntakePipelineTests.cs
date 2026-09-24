using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WorkOrderDesk.Intake;

namespace WorkOrderDesk.Tests;

// FakeLlmClient records whether it was touched. That is how we know the
// pipeline skipped the model on a confident rule hit.
public class IntakePipelineTests
{
    [Fact]
    public async Task SkipsModelWhenRulesAreSure()
    {
        var llm = new FakeLlmClient("{\"category\":\"other\",\"urgency\":\"low\",\"reply\":\"x\",\"reason\":\"y\"}");
        var pipeline = Create(llm, apiKey: "test-key");

        var result = await pipeline.SuggestAsync("no heat in unit 12 since last night");

        Assert.False(llm.WasCalled);
        Assert.Equal("hvac", result.Category);
        Assert.Equal("rules", result.Source);
    }

    [Fact]
    public async Task UsesModelWhenRulesAreUnsure()
    {
        var llm = new FakeLlmClient("""{"category":"electrical","urgency":"normal","reply":"We will look at the lights.","reason":"Flicker in the hall."}""");
        var pipeline = Create(llm, apiKey: "test-key");

        var result = await pipeline.SuggestAsync("something is off in 4B");

        Assert.True(llm.WasCalled);
        Assert.Equal("electrical", result.Category);
        Assert.Equal("model", result.Source);
    }

    [Fact]
    public async Task KeepsRulesWhenModelReturnsJunk()
    {
        var llm = new FakeLlmClient("not json at all");
        var pipeline = Create(llm, apiKey: "test-key");

        var result = await pipeline.SuggestAsync("something is off in 4B");

        Assert.True(llm.WasCalled);
        Assert.Equal("other", result.Category);
        Assert.Equal("fallback", result.Source);
    }

    [Fact]
    public async Task DoesNotCallModelWithoutAKey()
    {
        var llm = new FakeLlmClient("""{"category":"electrical","urgency":"normal","reply":"Hi.","reason":"Guess."}""");
        var pipeline = Create(llm, apiKey: "");

        var result = await pipeline.SuggestAsync("something is off in 4B");

        Assert.False(llm.WasCalled);
        Assert.Equal("rules", result.Source);
    }

    private static IntakePipeline Create(ILlmClient llm, string apiKey) =>
        new(
            new RuleClassifier(),
            llm,
            new SuggestionValidator(),
            Options.Create(new LlmOptions { ApiKey = apiKey }),
            NullLogger<IntakePipeline>.Instance);

    private sealed class FakeLlmClient : ILlmClient
    {
        private readonly string? _json;

        public FakeLlmClient(string? json)
        {
            _json = json;
        }

        public bool WasCalled { get; private set; }

        public Task<string?> CompleteJsonAsync(string body, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(_json);
        }
    }
}
