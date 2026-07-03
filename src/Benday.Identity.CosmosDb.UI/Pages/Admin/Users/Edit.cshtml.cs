using System.ComponentModel.DataAnnotations;
using Benday.Identity.CosmosDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Admin.Users;

[Authorize(Policy = CosmosIdentityConstants.AdminPolicyName)]
public class EditModel : PageModel
{
    private readonly UserManager<CosmosIdentityUser> _userManager;

    public EditModel(UserManager<CosmosIdentityUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool IsLocked { get; set; }

    public string? StatusMessage { get; set; }

    public string UserId { get; set; } = string.Empty;

    public class InputModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "First name")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Last name")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Phone number")]
        [Phone]
        public string? PhoneNumber { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        LoadUser(user);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            UserId = Input.Id;
            var u = await _userManager.FindByIdAsync(Input.Id);
            IsLocked = u != null && u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow;
            return Page();
        }

        var user = await _userManager.FindByIdAsync(Input.Id);
        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = Input.FirstName;
        user.LastName = Input.LastName;
        user.Email = Input.Email;
        user.UserName = Input.Email;
        user.PhoneNumber = Input.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            LoadUser(user);
            return Page();
        }

        StatusMessage = "User profile updated successfully.";
        LoadUser(user);
        return Page();
    }

    public async Task<IActionResult> OnPostToggleLockoutAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);
            StatusMessage = "User has been unlocked.";
        }
        else
        {
            user.LockoutEnd = DateTimeOffset.MaxValue;
            await _userManager.UpdateAsync(user);
            StatusMessage = "User has been locked.";
        }

        LoadUser(user);
        return Page();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            StatusMessage = "Password cannot be empty.";
            LoadUser(user);
            return Page();
        }

        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            StatusMessage = "Error removing password: " + string.Join(" ", removeResult.Errors.Select(e => e.Description));
            LoadUser(user);
            return Page();
        }

        var addResult = await _userManager.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
        {
            StatusMessage = "Error setting password: " + string.Join(" ", addResult.Errors.Select(e => e.Description));
            LoadUser(user);
            return Page();
        }

        StatusMessage = "Password has been reset successfully.";
        LoadUser(user);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            return RedirectToPage("/Admin/Users/Index");
        }

        StatusMessage = "Error deleting user: " + string.Join(" ", result.Errors.Select(e => e.Description));
        LoadUser(user);
        return Page();
    }

    private void LoadUser(CosmosIdentityUser user)
    {
        UserId = user.Id;
        IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        Input = new InputModel
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber
        };
    }
}
