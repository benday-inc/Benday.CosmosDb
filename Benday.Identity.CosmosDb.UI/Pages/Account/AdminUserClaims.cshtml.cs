using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Account;

[Authorize(Policy = "CosmosIdentityAdmin")]
public class AdminUserClaimsModel : PageModel
{
    private readonly UserManager<CosmosIdentityUser> _userManager;
    private readonly ICosmosDbClaimDefinitionStore _claimDefinitionStore;

    public AdminUserClaimsModel(
        UserManager<CosmosIdentityUser> userManager,
        ICosmosDbClaimDefinitionStore claimDefinitionStore)
    {
        _userManager = userManager;
        _claimDefinitionStore = claimDefinitionStore;
    }

    public CosmosIdentityUser TargetUser { get; set; } = default!;

    public IList<CosmosIdentityUserClaim> UserClaims { get; set; } = new List<CosmosIdentityUserClaim>();

    public IList<CosmosIdentityClaimDefinition> ClaimDefinitions { get; set; } = new List<CosmosIdentityClaimDefinition>();

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

    public async Task<IActionResult> OnPostAddAsync(string id, string claimType, string claimValue)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(claimType) && !string.IsNullOrWhiteSpace(claimValue))
        {
            user.Claims.Add(new CosmosIdentityUserClaim
            {
                ClaimType = claimType,
                ClaimValue = claimValue
            });

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = $"Claim '{claimType}' added successfully.";
            }
            else
            {
                StatusMessage = "Error: " + string.Join(" ", result.Errors.Select(e => e.Description));
            }
        }

        await LoadDataAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveAsync(string id, string claimType, string claimValue)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var claimToRemove = user.Claims.FirstOrDefault(c =>
            c.ClaimType == claimType && c.ClaimValue == claimValue);

        if (claimToRemove != null)
        {
            user.Claims.Remove(claimToRemove);
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = $"Claim '{claimType}' removed successfully.";
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
        UserClaims = user.Claims
            .Where(c => c.ClaimType != ClaimTypes.Role)
            .ToList();
        ClaimDefinitions = await _claimDefinitionStore.GetAllAsync();
    }
}
