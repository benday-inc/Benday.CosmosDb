using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<CosmosIdentityUser> _userManager;
    private readonly ICosmosIdentityEmailSender _emailSender;

    public ForgotPasswordModel(
        UserManager<CosmosIdentityUser> userManager,
        ICosmosIdentityEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool EmailSent { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);

        if (user != null && await _userManager.IsEmailConfirmedAsync(user))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { email = Input.Email, token = WebUtility.UrlEncode(token) },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                Input.Email,
                "Reset Password",
                $"Reset your password by <a href='{callbackUrl}'>clicking here</a>.");
        }

        // Always show the same message regardless of whether user exists (prevents account enumeration)
        EmailSent = true;
        return Page();
    }
}
