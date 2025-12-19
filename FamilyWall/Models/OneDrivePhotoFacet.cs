namespace FamilyWall.Models;

public sealed class OneDrivePhotoFacet
{
    public string? CameraMake { get; set; }

    public string? CameraModel { get; set; }

    public decimal? ExposureDenominator { get; set; }

    public decimal? ExposureNumerator { get; set; }

    public decimal? FNumber { get; set; }

    public decimal? FocalLength { get; set; }

    public int? Iso { get; set; }

    public int? Orientation { get; set; }

    public DateTimeOffset? TakenDateTime { get; set; }
}