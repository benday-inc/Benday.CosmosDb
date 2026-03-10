using Benday.Identity.CosmosDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Admin.Users;

[Authorize(Policy = CosmosIdentityConstants.AdminPolicyName)]
public class IndexModel : PageModel
{
    private readonly UserManager<CosmosIdentityUser> _userManager;
    private readonly IUserStore<CosmosIdentityUser> _userStore;

    public IndexModel(
        UserManager<CosmosIdentityUser> userManager,
        IUserStore<CosmosIdentityUser> userStore)
    {
        _userManager = userManager;
        _userStore = userStore;
    }

    public IList<CosmosIdentityUser> Users { get; set; } = new List<CosmosIdentityUser>();

    public string? Search { get; set; }

    public async Task<IActionResult> OnGetAsync(string? search)
    {
        Search = search;

        var queryableStore = (IQueryableUserStore<CosmosIdentityUser>)_userStore;
        var query = queryableStore.Users;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchUpper = search.ToUpper();
            query = query.Where(u =>
                u.Email.ToUpper().Contains(searchUpper) ||
                u.UserName.ToUpper().Contains(searchUpper) ||
                u.FirstName.ToUpper().Contains(searchUpper) ||
                u.LastName.ToUpper().Contains(searchUpper));
        }

        Users = query.Take(50).ToList();

        return Page();
    }
}
