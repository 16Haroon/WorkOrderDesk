using Microsoft.EntityFrameworkCore;
using WorkOrderDesk.Data;

var builder = WebApplication.CreateBuilder(args);

// Keep the sqlite file next to the project. A relative path would follow
// whichever folder the command was started from.
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "workorders.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Creates the table from WorkRequest on a brand new file.
    // It will not alter a table that is already there.
    db.Database.EnsureCreated();

    // One example so the first visit is not an empty list.
    if (!db.WorkRequests.Any())
    {
        db.WorkRequests.Add(new WorkRequest
        {
            Body = "the sink in 4B is dripping and there is water under the cabinet",
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}

// The site root just sends you to the list.
app.MapGet("/", () => Results.Redirect("/requests"));

app.MapGet("/requests", async (AppDbContext db) =>
{
    // Newest first. CreatedAt is stored in UTC.
    var requests = await db.WorkRequests
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    return Results.Ok(requests);
});

app.MapGet("/requests/{id:int}", async (int id, AppDbContext db) =>
{
    var request = await db.WorkRequests.FindAsync(id);

    // A missing id is a 404. An empty JSON object would look like a real row.
    return request is null ? Results.NotFound() : Results.Ok(request);
});

app.MapPost("/requests", async (NewRequest input, AppDbContext db) =>
{
    var body = input.Body?.Trim();
    if (string.IsNullOrEmpty(body))
    {
        return Results.BadRequest(new { error = "body is required" });
    }

    // The caller only sends the text. The id and the time are ours.
    var request = new WorkRequest
    {
        Body = body,
        CreatedAt = DateTime.UtcNow
    };

    db.WorkRequests.Add(request);
    await db.SaveChangesAsync();

    return Results.Created($"/requests/{request.Id}", request);
});

app.Run();

// The POST body. Kept apart from WorkRequest, which is the saved row.
public record NewRequest(string? Body);
