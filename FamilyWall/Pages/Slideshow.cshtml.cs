using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FamilyWall.Pages;

public class SlideshowModel(IWebHostEnvironment env) : PageModel
{
    public List<string> PhotoPaths { get; set; } = new();

    public void OnGet()
    {
        var photosFolder = Path.Combine(env.ContentRootPath, "photos");

        if (Directory.Exists(photosFolder))
        {
            var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            PhotoPaths = Directory.GetFiles(photosFolder)
                .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                .Select(f => $"/photos/{Path.GetFileName(f)}")
                .ToList();
        }
    }
}