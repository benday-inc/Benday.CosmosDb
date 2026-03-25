using Benday.Identity.CosmosDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Admin.ClaimDefinitions;

[Authorize(Policy = CosmosIdentityConstants.AdminPolicyName)]
public class IndexModel : PageModel
{
    private readonly ICosmosDbClaimDefinitionStore _store;

    public IndexModel(ICosmosDbClaimDefinitionStore store)
    {
        _store = store;
    }

    public IList<CosmosIdentityClaimDefinition> ClaimDefinitions { get; set; } = new List<CosmosIdentityClaimDefinition>();

    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ClaimDefinitions = await _store.GetAllAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            StatusMessage = "Invalid claim definition identifier.";
            ClaimDefinitions = await _store.GetAllAsync();
            return Page();
        }

        var claimDef = await _store.GetByIdAsync(CosmosIdentityConstants.SystemTenantId, id);

        if (claimDef == null)
        {
            StatusMessage = "Claim definition not found.";
            ClaimDefinitions = await _store.GetAllAsync();
            return Page();
        }

        await _store.DeleteAsync(claimDef);
        StatusMessage = $"Claim definition '{claimDef.ClaimType}' deleted successfully.";

        ClaimDefinitions = await _store.GetAllAsync();
        return Page();
    }
}
