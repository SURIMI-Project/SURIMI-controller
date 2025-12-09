namespace SURIMI.ConfigurationService.Models
{
    public class Geography
    {
        public RasterCellOrigin RasterCellOrigin { get; set; }
        public CoordinateReferenceSystem? Crs { get; set; }
        public double Xres { get; set; }
        public double Yres { get; set; }
        public int Ncol { get; set; }
        public int Nrow { get; set; }
        public double Xmin { get; set; }
        public double Xmax { get; set; }
        public double Ymin { get; set; }
        public double Ymax { get; set; }
    }
}
