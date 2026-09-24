using Microsoft.Extensions.Options;

namespace WorkOrderDesk.Intake;

// Owns the order of operations: rules, then maybe the model, then a fallback.
public class IntakePipeline
{
    private readonly RuleClassifier _rules;
    private readonly ILlmClient _llm;
    private readonly SuggestionValidator _validator;
    private readonly LlmOptions _options;
    private readonly ILogger<IntakePipeline> _logger;

    public IntakePipeline(
        RuleClassifier rules,
        ILlmClient llm,
        SuggestionValidator validator,
        IOptions<LlmOptions> options,
        ILogger<IntakePipeline> logger)
    {
        _rules = rules;
        _llm = llm;
        _validator = validator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Suggestion> SuggestAsync(string body, CancellationToken cancellationToken = default)
    {
        var fromRules = _rules.Classify(body);
        if (fromRules.Confident)
        {
            _logger.LogInformation("Using rules for this request.");
            return fromRules;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogInformation("Rules were unsure and no API key is set. Keeping the rule result.");
            return fromRules;
        }

        var json = await _llm.CompleteJsonAsync(body, cancellationToken);
        if (_validator.TryReadJson(json, out var fromModel) && fromModel is not null)
        {
            _logger.LogInformation("Using the model for this request.");
            return fromModel;
        }

        // Source becomes "fallback" so the review screen can show that we asked
        // and then ignored the answer. The category/urgency still come from rules.
        _logger.LogWarning("Model answer was unusable. Keeping the rule result.");
        return new Suggestion
        {
            Category = fromRules.Category,
            Urgency = fromRules.Urgency,
            Reply = fromRules.Reply,
            Reason = fromRules.Reason,
            Source = "fallback",
            Confident = false
        };
    }
}
