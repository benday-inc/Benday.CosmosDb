using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Benday.Identity.CosmosDb.UI.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly SignInManager<CosmosIdentityUser> _signInManager;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(SignInManager<CosmosIdentityUser> signInManager, ILogger<LogoutModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("Logging out user using SignInManager.SignOutAsync...");
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToPage();
    }
}
