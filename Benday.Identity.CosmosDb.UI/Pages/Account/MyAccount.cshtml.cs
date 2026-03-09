using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Account;

[Authorize]
public class MyAccountModel : PageModel
{
    private readonly UserManager<CosmosIdentityUser> _userManager;
    private readonly CosmosIdentityOptions _options;

    public MyAccountModel(
        UserManager<CosmosIdentityUser> userManager,
        CosmosIdentityOptions options)
    {
        _userManager = userManager;
        _options = options;
    }

    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool PasskeysEnabled { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var name = $"{user.FirstName} {user.LastName}".Trim();
        DisplayName = string.IsNullOrEmpty(name) ? user.Email : name;
        Email = user.Email;
        PasskeysEnabled = _options.EnablePasskeys;
        IsAdmin = User.IsInRole(_options.AdminRoleName);

        return Page();
    }
}
