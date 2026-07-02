using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Account;

[Authorize]
public class ManagePasskeysModel : PageModel
{
    private readonly UserManager<CosmosIdentityUser> _userManager;
    private readonly SignInManager<CosmosIdentityUser> _signInManager;
    private readonly CosmosIdentityOptions _options;

    public ManagePasskeysModel(
        UserManager<CosmosIdentityUser> userManager,
        SignInManager<CosmosIdentityUser> signInManager,
        CosmosIdentityOptions options)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _options = options;
    }

    public IList<UserPasskeyInfo> Passkeys { get; set; } = new List<UserPasskeyInfo>();

    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_options.EnablePasskeys)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        Passkeys = await _userManager.GetPasskeysAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostRegisterOptionsAsync()
    {
        if (!_options.EnablePasskeys)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var userName = await _userManager.GetUserNameAsync(user) ?? "User";

        var optionsJson = await _signInManager.MakePasskeyCreationOptionsAsync(new()
        {
            Id = userId,
            Name = userName,
            DisplayName = userName
        });

        return Content(optionsJson, "application/json");
    }

    public async Task<IActionResult> OnPostRegisterAsync(string credentialJson, string? passkeyName)
    {
        if (!_options.EnablePasskeys)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var attestationResult = await _signInManager.PerformPasskeyAttestationAsync(credentialJson);
        if (!attestationResult.Succeeded)
        {
            StatusMessage = "Failed to register passkey.";
            Passkeys = await _userManager.GetPasskeysAsync(user);
            return Page();
        }

        var passkey = attestationResult.Passkey;
        if (!string.IsNullOrEmpty(passkeyName))
        {
            passkey.Name = passkeyName;
        }

        var result = await _userManager.AddOrUpdatePasskeyAsync(user, passkey);
        if (!result.Succeeded)
        {
            StatusMessage = "Failed to store passkey.";
            Passkeys = await _userManager.GetPasskeysAsync(user);
            return Page();
        }

        StatusMessage = "Passkey registered successfully.";
        Passkeys = await _userManager.GetPasskeysAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string credentialId)
    {
        if (!_options.EnablePasskeys)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(credentialId))
        {
            StatusMessage = "Invalid passkey identifier.";
            Passkeys = await _userManager.GetPasskeysAsync(user);
            return Page();
        }

        // Decode the Base64Url credential ID to byte[]
        var credentialIdBytes = Convert.FromBase64String(
            credentialId.Replace('-', '+').Replace('_', '/').PadRight(
                credentialId.Length + (4 - credentialId.Length % 4) % 4, '='));

        await _userManager.RemovePasskeyAsync(user, credentialIdBytes);

        StatusMessage = "Passkey deleted successfully.";
        Passkeys = await _userManager.GetPasskeysAsync(user);
        return Page();
    }
}
