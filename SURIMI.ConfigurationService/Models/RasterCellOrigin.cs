namespace SURIMI.ConfigurationService.Models
{
    public enum RasterCellOrigin
    {
        Unspecified = 0,
        Centroid = 1, // Cell origin is at the center of the cell.
        TopLeft = 2,
        TopRight = 3,
        BottomLeft = 4,
        BottomRight = 5
    }
}
