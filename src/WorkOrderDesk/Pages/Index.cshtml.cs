using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkOrderDesk.Data;
using WorkOrderDesk.Intake;

namespace WorkOrderDesk.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IntakePipeline _pipeline;

    public IndexModel(AppDbContext db, IntakePipeline pipeline)
    {
        _db = db;
        _pipeline = pipeline;
    }

    public List<WorkRequest> Requests { get; private set; } = [];

    [BindProperty]
    public string? Body { get; set; }

    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        Requests = await _db.WorkRequests
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var body = Body?.Trim();
        if (string.IsNullOrEmpty(body))
        {
            Error = "Paste the resident's message first.";
            await OnGetAsync();
            return Page();
        }

        var suggestion = await _pipeline.SuggestAsync(body);
        var request = WorkRequest.FromIntake(body, suggestion);
        _db.WorkRequests.Add(request);
        await _db.SaveChangesAsync();

        return RedirectToPage("Request", new { id = request.Id });
    }
}
