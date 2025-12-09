namespace SURIMI.ConfigurationService.Models
{
    public class Items
    {
        public List<Market> Markets { get; set; } = [];
        public List<Currency> Currencies { get; set; } = [];
        public List<Species> Species { get; set; } = [];
        public List<FleetSegment> FleetSegments { get; set; } = [];
    }
}
