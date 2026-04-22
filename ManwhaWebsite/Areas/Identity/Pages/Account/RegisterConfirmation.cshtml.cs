using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManwhaWebsite.Areas.Identity.Pages.Account
{
    public class RegisterConfirmationModel : PageModel
    {
        public string? Email { get; set; }

        public void OnGet(string? email)
        {
            Email = email;
        }
    }
}
