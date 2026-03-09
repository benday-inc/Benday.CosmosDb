using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Account;

[Authorize(Policy = "CosmosIdentityAdmin")]
public class AdminRolesModel : PageModel
{
    private readonly RoleManager<CosmosIdentityRole> _roleManager;

    public AdminRolesModel(RoleManager<CosmosIdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public IList<CosmosIdentityRole> Roles { get; set; } = new List<CosmosIdentityRole>();

    public string? StatusMessage { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Roles = _roleManager.Roles.ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            ErrorMessage = "Role name cannot be empty.";
            Roles = _roleManager.Roles.ToList();
            return Page();
        }

        var result = await _roleManager.CreateAsync(new CosmosIdentityRole { Name = roleName });

        if (result.Succeeded)
        {
            StatusMessage = $"Role '{roleName}' created successfully.";
        }
        else
        {
            ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
        }

        Roles = _roleManager.Roles.ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string roleId)
    {
        if (string.IsNullOrEmpty(roleId))
        {
            ErrorMessage = "Invalid role identifier.";
            Roles = _roleManager.Roles.ToList();
            return Page();
        }

        var role = await _roleManager.FindByIdAsync(roleId);

        if (role == null)
        {
            ErrorMessage = "Role not found.";
            Roles = _roleManager.Roles.ToList();
            return Page();
        }

        var result = await _roleManager.DeleteAsync(role);

        if (result.Succeeded)
        {
            StatusMessage = $"Role '{role.Name}' deleted successfully.";
        }
        else
        {
            ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
        }

        Roles = _roleManager.Roles.ToList();
        return Page();
    }
}
