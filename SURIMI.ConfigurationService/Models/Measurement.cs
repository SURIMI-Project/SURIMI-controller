namespace SURIMI.ConfigurationService.Models
{
    public class Measurement
    {
        public string? System { get; set; }
        public List<UnitType> Units { get; set; } = [];
    }
}
