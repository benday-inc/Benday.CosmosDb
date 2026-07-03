using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Account;

public class ConfirmEmailModel : PageModel
{
    private readonly UserManager<CosmosIdentityUser> _userManager;

    public ConfirmEmailModel(UserManager<CosmosIdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public bool Confirmed { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            ErrorMessage = "Invalid email confirmation link.";
            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            ErrorMessage = "Invalid email confirmation link.";
            return Page();
        }

        var decodedToken = WebUtility.UrlDecode(token);
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        Confirmed = result.Succeeded;

        if (!result.Succeeded)
        {
            ErrorMessage = "Error confirming your email.";
        }

        return Page();
    }
}
