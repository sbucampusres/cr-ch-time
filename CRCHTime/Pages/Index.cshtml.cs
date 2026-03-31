using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Services;

namespace CRCHTime.Pages;

[Authorize(Policy = "RequireViewer")]
public class IndexModel : PageModel
{
    private readonly IApplicationContextService _appContextService;

    public IndexModel(IApplicationContextService appContextService)
    {
        _appContextService = appContextService;
    }

    public void OnGet()
    {
    }
}
