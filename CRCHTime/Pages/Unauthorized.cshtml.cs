using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRCHTime.Pages;

[AllowAnonymous]
public class UnauthorizedModel : PageModel
{
    public void OnGet()
    {
        // Simple unauthorized page - no processing needed
    }
}
