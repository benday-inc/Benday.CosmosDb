using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Admin.Users;

[Authorize(Policy = "CosmosIdentityAdmin")]
public class EditModel : PageModel
{
    private readonly UserManager<CosmosIdentityUser> _userManager;
    private readonly RoleManager<CosmosIdentityRole> _roleManager;

    public EditModel(
        UserManager<CosmosIdentityUser> userManager,
        RoleManager<CosmosIdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [BindProperty(SupportsGet = true)]
    public string Id { get; set; } = string.Empty;

    public CosmosIdentityUser? EditUser { get; set; }
    public IList<string> UserRoles { get; set; } = new List<string>();
    public IList<Claim> UserClaims { get; set; } = new List<Claim>();
    public List<string> AvailableRoles { get; set; } = new List<string>();
    public bool IsLockedOut { get; set; }
    public string? StatusMessage { get; set; }

    [BindProperty]
    public EditEmailInput EmailInput { get; set; } = new();

    [BindProperty]
    public AddClaimInput ClaimInput { get; set; } = new();

    [BindProperty]
    public AddRoleInput RoleInput { get; set; } = new();

    public class EditEmailInput
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class AddClaimInput
    {
        [Required]
        [Display(Name = "Claim type")]
        public string ClaimType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Claim value")]
        public string ClaimValue { get; set; } = string.Empty;
    }

    public class AddRoleInput
    {
        [Required]
        [Display(Name = "Role")]
        public string RoleName { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadUserAndReturn();
    }

    public async Task<IActionResult> OnPostUpdateEmailAsync()
    {
        var user = await _userManager.FindByIdAsync(Id);
        if (user == null) return NotFound();

        var email = EmailInput.Email;
        if (!new EmailAddressAttribute().IsValid(email))
        {
            StatusMessage = "Invalid email address.";
            return await LoadUserAndReturn();
        }

        user.Email = email;
        user.UserName = email;
        var result = await _userManager.UpdateAsync(user);
        StatusMessage = result.Succeeded
            ? "Email updated successfully."
            : string.Join(" ", result.Errors.Select(e => e.Description));

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostToggleLockoutAsync()
    {
        var user = await _userManager.FindByIdAsync(Id);
        if (user == null) return NotFound();

        // Prevent self-lockout
        var currentUserId = _userManager.GetUserId(User);
        if (user.Id == currentUserId)
        {
            StatusMessage = "You cannot lock out your own account.";
            return RedirectToPage(new { id = Id });
        }

        var isCurrentlyLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

        if (isCurrentlyLocked)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            StatusMessage = "User account has been unlocked.";
        }
        else
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            StatusMessage = "User account has been locked.";
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddRoleAsync()
    {
        var user = await _userManager.FindByIdAsync(Id);
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(RoleInput.RoleName))
        {
            var result = await _userManager.AddToRoleAsync(user, RoleInput.RoleName);
            StatusMessage = result.Succeeded
                ? $"Role '{RoleInput.RoleName}' added."
                : string.Join(" ", result.Errors.Select(e => e.Description));
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveRoleAsync(string roleName)
    {
        var user = await _userManager.FindByIdAsync(Id);
        if (user == null) return NotFound();

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        StatusMessage = result.Succeeded
            ? $"Role '{roleName}' removed."
            : string.Join(" ", result.Errors.Select(e => e.Description));

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddClaimAsync()
    {
        var user = await _userManager.FindByIdAsync(Id);
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(ClaimInput.ClaimType) &&
            !string.IsNullOrWhiteSpace(ClaimInput.ClaimValue))
        {
            var claim = new Claim(ClaimInput.ClaimType, ClaimInput.ClaimValue);
            var result = await _userManager.AddClaimAsync(user, claim);
            StatusMessage = result.Succeeded
                ? "Claim added."
                : string.Join(" ", result.Errors.Select(e => e.Description));
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveClaimAsync(string claimType, string claimValue)
    {
        var user = await _userManager.FindByIdAsync(Id);
        if (user == null) return NotFound();

        var claim = new Claim(claimType, claimValue);
        var result = await _userManager.RemoveClaimAsync(user, claim);
        StatusMessage = result.Succeeded
            ? "Claim removed."
            : string.Join(" ", result.Errors.Select(e => e.Description));

        return RedirectToPage(new { id = Id });
    }

    private async Task<IActionResult> LoadUserAndReturn()
    {
        var user = await _userManager.FindByIdAsync(Id);
        if (user == null) return NotFound();

        EditUser = user;
        EmailInput.Email = user.Email ?? string.Empty;
        UserRoles = await _userManager.GetRolesAsync(user);
        UserClaims = (await _userManager.GetClaimsAsync(user))
            .Where(c => c.Type != ClaimTypes.Role) // roles shown separately
            .ToList();

        var allRoleNames = _roleManager.Roles.Select(r => r.Name ?? "").ToList();
        AvailableRoles = allRoleNames.Except(UserRoles).ToList();

        IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

        return Page();
    }
}
