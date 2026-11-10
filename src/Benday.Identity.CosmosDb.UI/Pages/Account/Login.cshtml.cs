using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<CosmosIdentityUser> _signInManager;
    private readonly CosmosIdentityOptions _options;

    public LoginModel(
        SignInManager<CosmosIdentityUser> signInManager,
        CosmosIdentityOptions options)
    {
        _signInManager = signInManager;
        _options = options;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public string? PasskeyCredentialJson { get; set; }

    /// <summary>
    /// State of the "Remember me" checkbox at the time of a passkey sign-in.
    /// </summary>
    [BindProperty]
    public bool PasskeyRememberMe { get; set; }

    public bool AllowRegistration => _options.AllowRegistration;

    public bool ShowRememberMe => _options.ShowRememberMe;

    public bool EnablePasskeys => _options.EnablePasskeys;

    public string? LoginPageMessage => _options.LoginPageMessage;

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync()
    {
        // Clear any existing external cookie
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        Input.RememberMe = _options.RememberMeDefaultValue;
        PasskeyRememberMe = _options.RememberMeDefaultValue;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var isPersistent = _options.ShowRememberMe
            ? Input.RememberMe
            : _options.RememberMeDefaultValue;

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password,
            isPersistent: isPersistent, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LocalRedirect(ReturnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked out. Please try again later.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return Page();
    }

    public async Task<IActionResult> OnPostPasskeyOptionsAsync()
    {
        var optionsJson = await _signInManager.MakePasskeyRequestOptionsAsync(null);
        return Content(optionsJson, "application/json");
    }

    public async Task<IActionResult> OnPostPasskeySignInAsync()
    {
        if (string.IsNullOrEmpty(PasskeyCredentialJson))
        {
            ModelState.AddModelError(string.Empty, "Passkey authentication failed.");
            return Page();
        }

        // SignInManager.PasskeySignInAsync() always issues a session-only cookie
        // (isPersistent: false), so the steps are done here to honor "Remember me".
        PasskeyAssertionResult<CosmosIdentityUser> assertion;

        try
        {
            assertion = await _signInManager.PerformPasskeyAssertionAsync(PasskeyCredentialJson);
        }
        catch (InvalidOperationException)
        {
            // No assertion is underway: the short-lived challenge cookie expired
            // or was replaced by a newer request.
            ModelState.AddModelError(string.Empty, "Passkey sign-in expired. Please try again.");
            return Page();
        }

        if (!assertion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Passkey authentication failed.");
            return Page();
        }

        var user = assertion.User;
        var userManager = _signInManager.UserManager;

        // Store the updated sign count and authenticator data.
        var updateResult = await userManager.AddOrUpdatePasskeyAsync(user, assertion.Passkey);

        if (!updateResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Passkey authentication failed.");
            return Page();
        }

        if (!await _signInManager.CanSignInAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        if (userManager.SupportsUserLockout && await userManager.IsLockedOutAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Account locked out. Please try again later.");
            return Page();
        }

        var isPersistent = _options.ShowRememberMe
            ? PasskeyRememberMe
            : _options.RememberMeDefaultValue;

        // Same claim the framework adds for a passkey sign-in.
        await _signInManager.SignInWithClaimsAsync(
            user, isPersistent, new[] { new Claim("amr", "pwd") });

        return LocalRedirect(ReturnUrl ?? "/");
    }
}
