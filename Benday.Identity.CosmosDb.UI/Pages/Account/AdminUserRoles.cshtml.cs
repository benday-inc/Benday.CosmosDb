using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Account;

[Authorize(Policy = "CosmosIdentityAdmin")]
public class AdminUserRolesModel : PageModel
{
    private readonly UserManager<CosmosIdentityUser> _userManager;
    private readonly RoleManager<CosmosIdentityRole> _roleManager;

    public AdminUserRolesModel(
        UserManager<CosmosIdentityUser> userManager,
        RoleManager<CosmosIdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public CosmosIdentityUser TargetUser { get; set; } = default!;

    public IList<string> AssignedRoles { get; set; } = new List<string>();

    public IList<CosmosIdentityRole> AllRoles { get; set; } = new List<CosmosIdentityRole>();

    public string? StatusMessage { get; set; }

    public string UserId { get; set; } = string.Empty;

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

        await LoadDataAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync(string id, string roleName)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                StatusMessage = $"Role '{roleName}' added successfully.";
            }
            else
            {
                StatusMessage = "Error: " + string.Join(" ", result.Errors.Select(e => e.Description));
            }
        }

        await LoadDataAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveAsync(string id, string roleName)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                StatusMessage = $"Role '{roleName}' removed successfully.";
            }
            else
            {
                StatusMessage = "Error: " + string.Join(" ", result.Errors.Select(e => e.Description));
            }
        }

        await LoadDataAsync(user);
        return Page();
    }

    private async Task LoadDataAsync(CosmosIdentityUser user)
    {
        TargetUser = user;
        UserId = user.Id;
        AssignedRoles = await _userManager.GetRolesAsync(user);
        AllRoles = _roleManager.Roles.ToList();
    }
}
