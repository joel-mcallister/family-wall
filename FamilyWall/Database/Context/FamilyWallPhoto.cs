using FamilyWall.Models;

namespace FamilyWall.Database.Context;

public sealed class FamilyWallPhoto : OneDriveItem
{
    public string FileName { get; set; }
    public bool? IsDeleted { get; set; } = false;

    public int? DisplayCount { get; set; } = 0;
}