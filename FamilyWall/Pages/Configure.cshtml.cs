using System.Net.Mime;
using FamilyWall.Database.Entities;
using FamilyWall.Database.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FamilyWall.Pages;

public class ConfigureModel(IFamilyWallDataContext db) : PageModel
{
    public required List<SelectListItem> Backgrounds { get; set; } = db.Backgrounds.FindAll().Select(x => new SelectListItem(x.Name, x.FileName)).ToList();

    public required List<FamilyWallTaskIconMapping> TaskIconMappings { get; set; } = db.TaskIconMappings.FindAll().ToList();

    [BindProperty]
    public required string Background { get; set; }

    [BindProperty]
    public string IconName { get; set; }

    [BindProperty]
    public string Icon { get; set; }

    [BindProperty]
    public string Keywords { get; set; }

    public IActionResult OnGetMainCss()
    {
        FamilyWallConfiguration? config = db.Configuration.FindOne(x => x.Id == 1);
        if (config == null)
        {
            config = new FamilyWallConfiguration
            {
                Background = "summer.png",
                Id = 1,
                Name = "Family Wall"
            };
            db.Configuration.Upsert(config);
        }
        Background = config.Background;

        return new ContentResult
        {
            Content = "body { background-image: url('/img/backgrounds/" + Background + "');}",
            ContentType = MediaTypeNames.Text.Css,
            StatusCode = 200
        };
    }

    public void OnGet()
    {
        FamilyWallConfiguration? config = db.Configuration.FindOne(x => x.Id == 1);
        if (config == null)
        {
            config = new FamilyWallConfiguration
            {
                Background = "summer.png",
                Id = 1,
                Name = "Family Wall"
            };
            db.Configuration.Upsert(config);
        }
        Background = config.Background;

        List<FamilyWallTaskIconMapping>? xx = db.TaskIconMappings.FindAll().ToList();
    }

    public IActionResult OnPost()
    {
        FamilyWallConfiguration? config = db.Configuration.FindOne(x => x.Id == 1);
        config.Background = Background;
        db.Configuration.Upsert(config);

        var trim = Icon.Replace("<i class=\"", null).Replace("\"></i>", null).Trim();

        db.TaskIconMappings.Upsert(new FamilyWallTaskIconMapping
        {
            Name = IconName.Trim(),
            Icon = trim,
            Keywords = Keywords.ToLowerInvariant().Split(',').ToList()
        });

        return RedirectToPage("Configure");
    }
}