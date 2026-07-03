using Benday.Identity.CosmosDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Admin;

[Authorize(Policy = CosmosIdentityConstants.AdminPolicyName)]
public class DashboardModel : PageModel
{
    private readonly CosmosIdentityOptions _options;

    public DashboardModel(CosmosIdentityOptions options)
    {
        _options = options;
    }

    public bool PasskeysEnabled => _options.EnablePasskeys;

    public void OnGet()
    {
    }
}
