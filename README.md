# WorkOrderDesk

WorkOrderDesk is a small intake app for building maintenance.

A resident sends a sentence like "the sink in 4B is dripping and there is water under the cabinet." Before anyone is dispatched, someone has to put a category on it (plumbing, electrical, and so on), pick how urgent it is, and write a short reply. This project does that first pass and then waits for a person to confirm.

Clear cases are handled with ordinary C# string checks. A leak, no heat, or a gas smell does not need a model. Vague notes can be sent to an LLM if you have configured a key. Whatever comes back has to match a fixed list of labels, or it is thrown away. The original guess is stored next to the decision, so you can see where they differ.

## Why this stack

ASP.NET Core hosts the pages and a small JSON API. Razor pages are the inbox and the review screen. Entity Framework Core maps `WorkRequest` to a SQLite file, so `dotnet run` is enough; there is no database server to install. The same entity is what you would point at SQL Server later.

The LLM call is a single HTTP request to an OpenAI-compatible chat endpoint. Tests never make that call. They use a fake client.

## Run

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download), then from this folder:

```
dotnet test
dotnet run --project src/WorkOrderDesk
```

Open http://localhost:5080

The first launch creates `src/WorkOrderDesk/workorders.db` and files one sample request so the inbox is not empty. That file is gitignored.

If you ran the earlier version that only stored the message text, delete `workorders.db` once. `EnsureCreated` will not add the new columns to an old table.

### Try it (no API key)

Paste these on the home page, one at a time:

1. `the sink in 4B is dripping and there is water under the cabinet` — plumbing, urgent
2. `no heat in unit 12 since last night` — hvac, urgent
3. `I smell gas in the kitchen` — emergency. The rules raise urgency and leave the trade as other.

Open the request and hit **Save decision**, or change the category first.

`something is off in 4B` is not a confident match. With no key you still get the rule result; with a key that one can go to the model.

### Optional model

The key is not in the repo. From `src/WorkOrderDesk`:

```
dotnet user-secrets init
dotnet user-secrets set "Llm:ApiKey" "your-key"
```

`Llm__ApiKey` as an environment variable also works. Base URL and model live in `appsettings.json` and default to OpenAI plus `gpt-4o-mini`. Point `Llm:BaseUrl` at another OpenAI-compatible host if you need to. Leave the key blank and the model is never called.

Same flow over HTTP:

```
curl -X POST http://localhost:5080/api/requests \
  -H "Content-Type: application/json" \
  -d '{"body":"no heat in unit 12 since last night"}'
```

```
curl -X POST http://localhost:5080/api/requests/1/decision \
  -H "Content-Type: application/json" \
  -d '{"category":"hvac","urgency":"urgent","reply":"We will send someone this morning."}'
```

## Layout

- `src/WorkOrderDesk/Intake/RuleClassifier.cs` — phrase matching for category and urgency
- `src/WorkOrderDesk/Intake/IntakePipeline.cs` — rules first; the model only if those are unsure
- `src/WorkOrderDesk/Intake/OpenAiCompatibleClient.cs` — the chat HTTP call
- `src/WorkOrderDesk/Intake/SuggestionValidator.cs` — rejects unknown labels and empty replies
- `src/WorkOrderDesk/Data/WorkRequest.cs` — the row: message, suggestion, and the person's call
- `src/WorkOrderDesk/Pages/` — inbox and review
- `src/WorkOrderDesk/Program.cs` — startup, SQLite, JSON routes
- `tests/WorkOrderDesk.Tests/` — rules and pipeline, with a fake LLM

## Limits

There is no login. The database is a file. The rules are a phrase list, not a trained model. That is enough to walk through the workflow; it is not a dispatch system.
