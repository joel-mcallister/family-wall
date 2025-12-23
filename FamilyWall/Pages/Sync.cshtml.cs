using FamilyWall.Database.Interfaces;
using FamilyWall.Models;
using FamilyWall.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace FamilyWall.Pages;

[Authorize]
[AuthorizeForScopes(Scopes = ["Files.Read", "Tasks.ReadWrite"])]
public class SyncModel(
    OneDriveImageService oneDriveImageService,
    ITokenAcquisition tokenAcquisition,
    IWebHostEnvironment env,
    IFamilyWallDataContext db,
    MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler,
    ILogger<IndexModel> logger) : PageModel
{
    public required List<OneDriveItem> AllPhotos { get; set; }

    public async Task<IActionResult> OnGetSyncPhoto(string id)
    {
        return await oneDriveImageService.OnGetSyncPhoto(id);
    }

    public async Task<IActionResult> OnGet()
    {
        try
        {
            oneDriveImageService.ClearCache();
            string token = await tokenAcquisition.GetAccessTokenForUserAsync(["Files.Read", "Tasks.ReadWrite"]);
            AllPhotos = await oneDriveImageService.SyncPhotos(token);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            logger.LogWarning($"Sending Challenge");
            consentHandler.HandleException(ex);
            return Challenge();
        }

        try
        {
            var photosFolder = Path.Combine(env.ContentRootPath, "wwwroot", "img", "backgrounds");
            Directory.CreateDirectory(photosFolder);

            var files = Directory.GetFiles(photosFolder);

            foreach (var file in files)
            {
                db.Backgrounds.Upsert(new FamilyWallBackgrounds
                {
                    FileName = Path.GetFileName(file),
                    Name = Path.GetFileNameWithoutExtension(file).Replace("-", " ")
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Can't write to database");
        }

        return Page();
    }
}