using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkOrderDesk.Data;
using WorkOrderDesk.Intake;

namespace WorkOrderDesk.Pages;

public class RequestModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly SuggestionValidator _validator;

    public RequestModel(AppDbContext db, SuggestionValidator validator)
    {
        _db = db;
        _validator = validator;
    }

    public WorkRequest? Item { get; private set; }

    [BindProperty]
    public string? Category { get; set; }

    [BindProperty]
    public string? Urgency { get; set; }

    [BindProperty]
    public string? Reply { get; set; }

    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Item = await _db.WorkRequests.FindAsync(id);
        if (Item is null)
        {
            return NotFound();
        }

        // Prefer the saved decision when they come back to a reviewed row.
        Category = Item.FinalCategory ?? Item.SuggestedCategory;
        Urgency = Item.FinalUrgency ?? Item.SuggestedUrgency;
        Reply = Item.FinalReply ?? Item.SuggestedReply;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Item = await _db.WorkRequests.FindAsync(id);
        if (Item is null)
        {
            return NotFound();
        }

        if (!_validator.TryDecision(Category, Urgency, Reply, out var suggestion)
            || suggestion is null)
        {
            Error = "Pick a known category and urgency, and leave a short reply.";
            return Page();
        }

        Item.ApplyDecision(suggestion.Category, suggestion.Urgency, suggestion.Reply);
        await _db.SaveChangesAsync();
        return RedirectToPage("Request", new { id });
    }
}
