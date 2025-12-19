using FamilyWall.Models;
using FamilyWall.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace FamilyWall.Pages;

[Authorize]
[AuthorizeForScopes(Scopes = new[] { "Files.Read", "Tasks.ReadWrite" })]
public class SyncModel(
    OneDriveRandomImageService oneDriveRandomImageService,
    ITokenAcquisition tokenAcquisition,
    IConfiguration configuration,
    MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler,
    ILogger<IndexModel> logger) : PageModel
{
    public required List<OneDriveItem> AllPhotos { get; set; }

    public async Task<IActionResult> OnGetSyncPhoto(string id)
    {
        return await oneDriveRandomImageService.OnGetSyncPhoto(id);
    }

    public async Task<IActionResult> OnGet()
    {
        try
        {
            string token = await tokenAcquisition.GetAccessTokenForUserAsync(["Files.Read", "Tasks.ReadWrite"]);
            AllPhotos = await oneDriveRandomImageService.SyncPhotos(token);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            logger.LogWarning($"Sending Challenge");
            consentHandler.HandleException(ex);
            return Challenge();
        }

        return Page();
    }
}