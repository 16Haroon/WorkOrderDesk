namespace WorkOrderDesk.Intake;

// First pass over the resident's text. Cheap and deterministic.
// If this is confident, IntakePipeline never calls the model.
public class RuleClassifier
{
    public Suggestion Classify(string body)
    {
        var text = body.ToLowerInvariant();

        var category = MatchCategory(text);
        var urgency = MatchUrgency(text);

        // Sure enough to skip the model:
        //   - we know the trade and it is more than a routine job, or
        //   - we recognized a fixture by name (sink, furnace, ...), or
        //   - it is an emergency even if the trade is still "other" (gas smell).
        // "something is off in 4B" hits none of those.
        var confident =
            (category != "other" && urgency != "normal")
            || (category != "other" && HasClearFixture(text))
            || urgency == "emergency";

        return new Suggestion
        {
            Category = category,
            Urgency = urgency,
            Reply = BuildReply(category, urgency),
            Reason = BuildReason(category, urgency, confident),
            Source = "rules",
            Confident = confident
        };
    }

    private static string MatchCategory(string text)
    {
        if (ContainsAny(text, "sink", "toilet", "faucet", "drain", "pipe", "leak", "drip", "dripping", "water heater"))
        {
            return "plumbing";
        }

        if (ContainsAny(text, "outlet", "breaker", "spark", "sparking", "wiring", "lights out", "no power"))
        {
            return "electrical";
        }

        if (ContainsAny(text, "no heat", "furnace", "thermostat", "boiler", "air conditioning", "the ac", "ac is", "hvac"))
        {
            return "hvac";
        }

        // "heat" by itself shows up in ordinary English. Only treat it as HVAC
        // when the message is clearly about a unit.
        if (text.Contains("heat") && ContainsAny(text, "unit", "apartment", "apt", "radiator"))
        {
            return "hvac";
        }

        if (ContainsAny(text, "fridge", "refrigerator", "dishwasher", "washer", "dryer", "stove", "oven", "microwave"))
        {
            return "appliance";
        }

        return "other";
    }

    private static string MatchUrgency(string text)
    {
        if (ContainsAny(text, "gas smell", "smell gas", "carbon monoxide", "on fire", "sparking", "flooding", "flood"))
        {
            return "emergency";
        }

        if (ContainsAny(text, "no heat", "no hot water", "water under", "overflowing", "won't stop", "will not stop", "pouring"))
        {
            return "urgent";
        }

        return "normal";
    }

    private static bool HasClearFixture(string text) =>
        ContainsAny(text, "sink", "toilet", "faucet", "furnace", "fridge", "dishwasher", "outlet", "breaker");

    private static string BuildReply(string category, string urgency)
    {
        if (urgency == "emergency")
        {
            return "This looks like an emergency. Get somewhere safe if you need to. We are treating it as " + category + " and will send someone now.";
        }

        if (urgency == "urgent")
        {
            return "We will treat this as " + category + " and send someone as soon as we can. If it gets worse, call the office.";
        }

        return "Logged as " + category + ". A tech will be scheduled in the normal queue.";
    }

    private static string BuildReason(string category, string urgency, bool confident)
    {
        if (!confident)
        {
            return "The message does not point at a clear fixture or a clear urgency.";
        }

        return "Matched " + category + " and " + urgency + " from the wording.";
    }

    private static bool ContainsAny(string text, params string[] phrases)
    {
        foreach (var phrase in phrases)
        {
            if (text.Contains(phrase))
            {
                return true;
            }
        }

        return false;
    }
}
