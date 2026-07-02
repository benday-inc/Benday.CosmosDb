using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Benday.Identity.CosmosDb.UI.Pages.Account;

[Authorize]
public class EditProfileModel : PageModel
{
    private readonly UserManager<CosmosIdentityUser> _userManager;
    private readonly SignInManager<CosmosIdentityUser> _signInManager;

    public EditProfileModel(
        UserManager<CosmosIdentityUser> userManager,
        SignInManager<CosmosIdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string Email { get; set; } = string.Empty;

    public bool ProfileUpdated { get; set; }

    public class InputModel
    {
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

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        Email = user.Email;
        Input = new InputModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            Email = user.Email;
            return Page();
        }

        user.FirstName = Input.FirstName;
        user.LastName = Input.LastName;
        user.PhoneNumber = Input.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            Email = user.Email;
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        Email = user.Email;
        ProfileUpdated = true;
        return Page();
    }
}
