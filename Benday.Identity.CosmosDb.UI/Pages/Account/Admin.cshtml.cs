using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Account;

[Authorize(Policy = "CosmosIdentityAdmin")]
public class AdminModel : PageModel
{
    private readonly CosmosIdentityOptions _options;

    public AdminModel(CosmosIdentityOptions options)
    {
        _options = options;
    }

    public bool PasskeysEnabled => _options.EnablePasskeys;

    public void OnGet()
    {
    }
}
