using FamilyWall.Database.Entities;
using FamilyWall.Database.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FamilyWall.Pages;

public class ConfigureModel(IFamilyWallDataContext db) : PageModel
{
    public required List<SelectListItem> Backgrounds { get; set; } = db.Backgrounds.FindAll().Select(x => new SelectListItem(x.Name, x.FileName)).ToList();

    [BindProperty]
    public required string Background { get; set; }

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
    }

    public IActionResult OnPost()
    {
        FamilyWallConfiguration? config = db.Configuration.FindOne(x => x.Id == 1);
        config.Background = Background;
        db.Configuration.Upsert(config);

        return RedirectToPage("Configure");
    }
}