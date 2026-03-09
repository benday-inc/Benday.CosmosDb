using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Account;

[Authorize(Policy = "CosmosIdentityAdmin")]
public class AdminClaimDefinitionEditModel : PageModel
{
    private readonly ICosmosDbClaimDefinitionStore _store;

    public AdminClaimDefinitionEditModel(ICosmosDbClaimDefinitionStore store)
    {
        _store = store;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool IsNew { get; set; } = true;

    public string? StatusMessage { get; set; }

    public class InputModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Claim type is required.")]
        public string ClaimType { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string AllowedValuesText { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            var claimDef = await _store.GetByIdAsync(CosmosIdentityConstants.SystemOwnerId, id);

            if (claimDef == null)
            {
                return RedirectToPage("AdminClaimDefinitions");
            }

            IsNew = false;
            Input = new InputModel
            {
                Id = claimDef.Id,
                ClaimType = claimDef.ClaimType,
                Description = claimDef.Description,
                AllowedValuesText = string.Join("\n", claimDef.AllowedValues)
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            IsNew = string.IsNullOrEmpty(Input.Id);
            return Page();
        }

        CosmosIdentityClaimDefinition claimDef;

        if (!string.IsNullOrEmpty(Input.Id))
        {
            claimDef = await _store.GetByIdAsync(CosmosIdentityConstants.SystemOwnerId, Input.Id)
                        ?? new CosmosIdentityClaimDefinition();
            IsNew = false;
        }
        else
        {
            claimDef = new CosmosIdentityClaimDefinition();
            IsNew = true;
        }

        claimDef.ClaimType = Input.ClaimType.Trim();
        claimDef.Description = Input.Description?.Trim() ?? string.Empty;
        claimDef.AllowedValues = string.IsNullOrWhiteSpace(Input.AllowedValuesText)
            ? new List<string>()
            : Input.AllowedValuesText
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList();

        await _store.SaveAsync(claimDef);

        StatusMessage = IsNew ? "Claim definition created successfully." : "Claim definition updated successfully.";
        IsNew = false;
        Input.Id = claimDef.Id;

        return Page();
    }
}
