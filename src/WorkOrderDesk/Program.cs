using Microsoft.EntityFrameworkCore;
using WorkOrderDesk.Data;
using WorkOrderDesk.Intake;

var builder = WebApplication.CreateBuilder(args);

// Pin the file to the project folder. A relative path would follow
// whichever directory you happened to start the process from.
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "workorders.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));
builder.Services.AddSingleton<RuleClassifier>();
builder.Services.AddSingleton<SuggestionValidator>();
builder.Services.AddScoped<IntakePipeline>();

// Eight seconds is plenty for a short JSON reply. After that we keep the rules.
builder.Services.AddHttpClient<ILlmClient, OpenAiCompatibleClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseStaticFiles();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var pipeline = scope.ServiceProvider.GetRequiredService<IntakePipeline>();

    DropOldSchemaIfNeeded(db);
    db.Database.EnsureCreated();

    // One sample row so the first visit is not a blank inbox.
    if (!db.WorkRequests.Any())
    {
        var body = "the sink in 4B is dripping and there is water under the cabinet";
        var suggestion = pipeline.SuggestAsync(body).GetAwaiter().GetResult();
        db.WorkRequests.Add(WorkRequest.FromIntake(body, suggestion));
        db.SaveChanges();
    }
}

app.MapRazorPages();

app.MapGet("/api/requests", async (AppDbContext db) =>
{
    var requests = await db.WorkRequests
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    return Results.Ok(requests);
});

app.MapGet("/api/requests/{id:int}", async (int id, AppDbContext db) =>
{
    var request = await db.WorkRequests.FindAsync(id);
    return request is null ? Results.NotFound() : Results.Ok(request);
});

app.MapPost("/api/requests", async (NewRequest input, AppDbContext db, IntakePipeline pipeline) =>
{
    var body = input.Body?.Trim();
    if (string.IsNullOrEmpty(body))
    {
        return Results.BadRequest(new { error = "body is required" });
    }

    var suggestion = await pipeline.SuggestAsync(body);
    var request = WorkRequest.FromIntake(body, suggestion);
    db.WorkRequests.Add(request);
    await db.SaveChangesAsync();

    return Results.Created($"/api/requests/{request.Id}", request);
});

app.MapPost("/api/requests/{id:int}/decision", async (
    int id,
    Decision input,
    AppDbContext db,
    SuggestionValidator validator) =>
{
    var request = await db.WorkRequests.FindAsync(id);
    if (request is null)
    {
        return Results.NotFound();
    }

    if (!validator.TryDecision(input.Category, input.Urgency, input.Reply, out var suggestion)
        || suggestion is null)
    {
        return Results.BadRequest(new { error = "category, urgency, and reply must be valid" });
    }

    request.ApplyDecision(suggestion.Category, suggestion.Urgency, suggestion.Reply);
    await db.SaveChangesAsync();
    return Results.Ok(request);
});

app.Run();

// The first commit of this app only had Body and CreatedAt.
// EnsureCreated will not add columns, so that old file has to go.
static void DropOldSchemaIfNeeded(AppDbContext db)
{
    var conn = db.Database.GetDbConnection();
    conn.Open();
    try
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='WorkRequests'";
        if (cmd.ExecuteScalar() is null)
        {
            return;
        }

        cmd.CommandText = "PRAGMA table_info(WorkRequests)";
        using var reader = cmd.ExecuteReader();
        var hasSuggestion = false;
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), "SuggestedCategory", StringComparison.OrdinalIgnoreCase))
            {
                hasSuggestion = true;
                break;
            }
        }

        reader.Close();
        if (!hasSuggestion)
        {
            conn.Close();
            db.Database.EnsureDeleted();
        }
    }
    finally
    {
        if (conn.State == System.Data.ConnectionState.Open)
        {
            conn.Close();
        }
    }
}

public record NewRequest(string? Body);

public record Decision(string? Category, string? Urgency, string? Reply);
