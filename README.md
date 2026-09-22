# WorkOrderDesk

WorkOrderDesk is the desk where a maintenance request lands before anyone is sent out.

A resident writes a sentence. "The sink in 4B is dripping and there is water under the cabinet." Somebody has to keep that message, then decide what kind of job it is and how soon a tech should go. This version keeps the message. You paste it, it is saved, and you can open the list again later.

The sorting comes next, and it is worth being careful about. A leak or no heat is obvious, and those should be decided with a few plain rules. A vague message is the one worth asking a model about. Either way, a person still confirms it before a tech is dispatched. That is the shape of the app. Only the saving is built so far.

## Why this setup

It is one ASP.NET Core project. A request is a row, an id, the text, and a time, so Entity Framework Core maps the `WorkRequest` class onto a table. SQLite stores that table in a single file. Clone the repo, run it, and the database is there. Nothing else to install. The same class is what would talk to SQL Server if more than one person needed the data.

## Run

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). From this folder:

```
dotnet run --project src/WorkOrderDesk
```

Open http://localhost:5080/requests

You should see one saved request. The app writes that example the first time the database is empty, so the list is not blank. The database is `src/WorkOrderDesk/workorders.db`. It shows up on first run and is not part of the repo.

Add another:

```
curl -X POST http://localhost:5080/requests \
  -H "Content-Type: application/json" \
  -d '{"body":"no heat in unit 12 since last night"}'
```

Refresh the list. The new one is at the top. A blank message comes back as 400.

One request on its own is http://localhost:5080/requests/1

## Where to look

- `src/WorkOrderDesk/Program.cs` starts the app, creates the database, and defines the three endpoints.
- `src/WorkOrderDesk/Data/WorkRequest.cs` is one saved request.
- `src/WorkOrderDesk/Data/AppDbContext.cs` is how Entity Framework reaches the file.
