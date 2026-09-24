using WorkOrderDesk.Intake;

namespace WorkOrderDesk.Tests;

// These four messages are the ones in the README. If a rule change breaks
// one of them, the writeup is wrong too.
public class RuleClassifierTests
{
    private readonly RuleClassifier _rules = new();

    [Fact]
    public void DrippingSinkWithWaterUnderCabinet_IsPlumbingUrgent()
    {
        var result = _rules.Classify("the sink in 4B is dripping and there is water under the cabinet");

        Assert.Equal("plumbing", result.Category);
        Assert.Equal("urgent", result.Urgency);
        Assert.True(result.Confident);
        Assert.Equal("rules", result.Source);
    }

    [Fact]
    public void NoHeatOvernight_IsHvacUrgent()
    {
        var result = _rules.Classify("no heat in unit 12 since last night");

        Assert.Equal("hvac", result.Category);
        Assert.Equal("urgent", result.Urgency);
        Assert.True(result.Confident);
    }

    [Fact]
    public void GasSmell_IsEmergency()
    {
        var result = _rules.Classify("I smell gas in the kitchen");

        Assert.Equal("emergency", result.Urgency);
        Assert.True(result.Confident);
    }

    [Fact]
    public void VagueNote_IsNotConfident()
    {
        var result = _rules.Classify("something is off in 4B");

        Assert.Equal("other", result.Category);
        Assert.Equal("normal", result.Urgency);
        Assert.False(result.Confident);
    }
}
