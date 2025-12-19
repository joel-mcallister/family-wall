namespace FamilyWall.Models;

public sealed class OneDriveItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? WebUrl { get; set; }
    public OneDriveFileFacet? File { get; set; }
    public OneDrivePhotoFacet? Photo { get; set; }
    public OneDriveFileSystemInfoFacet? FileSystemInfo { get; set; }
    public DateTimeOffset? CreatedDateTime { get; set; }
    public OneDriveItemImageFacet? Image { get; set; }

    public OneDriveItemLocation? Location { get; set; }

}