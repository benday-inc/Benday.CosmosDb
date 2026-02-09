using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Admin.Users;

[Authorize(Policy = "CosmosIdentityAdmin")]
public class IndexModel : PageModel
{
    private readonly UserManager<CosmosIdentityUser> _userManager;

    public IndexModel(UserManager<CosmosIdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public List<CosmosIdentityUser> Users { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 25;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public void OnGet()
    {
        // Materialize to list for safe in-memory filtering (Cosmos DB LINQ has limited operator support)
        var allUsers = _userManager.Users.OrderBy(u => u.Email).ToList();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim().ToUpperInvariant();
            allUsers = allUsers
                .Where(u =>
                    (u.NormalizedEmail ?? "").Contains(term) ||
                    (u.NormalizedUserName ?? "").Contains(term))
                .ToList();
        }

        TotalCount = allUsers.Count;

        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

        Users = allUsers
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }
}
