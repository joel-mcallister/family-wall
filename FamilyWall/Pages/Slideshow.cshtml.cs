using FamilyWall.Database.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace FamilyWall.Pages;

public class SlideshowModel(
    IWebHostEnvironment env,
    IFamilyWallDataContext db) : PageModel
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

    public IActionResult OnGetDelete(string file)
    {
        var photo = db.Photos.FindOne(x => x.FileName == Path.GetFileName(file));
        photo.IsDeleted = true;
        db.Photos.Upsert(photo);

        var photosFolder = Path.Combine(env.ContentRootPath, "photos");
        var image = Path.Combine(photosFolder, Path.GetFileName(file));
        var json = Path.Combine(photosFolder, $"{Path.GetFileNameWithoutExtension(file)}.json");

        if (System.IO.File.Exists(image))
        {
            System.IO.File.Delete(image);
            System.IO.File.Delete(json);
        }

        return new OkResult();
    }

    // GET /Slideshow?handler=Rotate&file=example.webp&dir=left
    public IActionResult OnGetRotate(string file, string dir)
    {
        // sanitize and validate file name
        var fileName = Path.GetFileName(file ?? string.Empty);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest("Missing 'file' query parameter.");
        }

        var ext = Path.GetExtension(fileName);
        if (!string.Equals(ext, ".jpg", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only '.jpg' images are supported by this handler.");
        }

        var photosFolder = Path.Combine(env.ContentRootPath, "photos");
        var filePath = Path.Combine(photosFolder, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        // determine rotation angle
        if (string.IsNullOrWhiteSpace(dir))
        {
            return BadRequest("Missing 'dir' query parameter. Use 'left' or 'right'.")
;
        }

        var dirNormalized = dir.Trim().ToLowerInvariant();
        float angle = dirNormalized switch
        {
            "left" or "l" => -90f,
            "right" or "r" => 90f,
            _ => float.NaN
        };

        if (float.IsNaN(angle))
        {
            return BadRequest("Invalid 'dir' value. Use 'left' or 'right'.");
        }

        try
        {
            // Load, rotate, and save to a temp file then replace original to avoid partial writes
            using (var image = Image.Load(filePath))
            {
                image.Mutate(x => x.Rotate(angle));

                var tempPath = filePath + ".tmp";
                // Ensure any previous temp file is removed
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }

                using (var outStream = System.IO.File.Open(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    image.Save(outStream, new JpegEncoder());
                }

                // Atomically replace original with temp (if platform supports it)
                // If Replace throws on non-supported platforms, fall back to delete+move.
                try
                {
                    System.IO.File.Replace(tempPath, filePath, null);
                }
                catch
                {
                    // fallback
                    System.IO.File.Delete(filePath);
                    System.IO.File.Move(tempPath, filePath);
                }
            }

            // Return the rotated image
            return new OkResult();
        }
        catch
        {
            return StatusCode(500, "Failed to rotate the image.");
        }
    }
}