namespace SURIMI.ConfigurationService.Models
{
    public class Standards
    {
        public string? DateAndTime { get; set; }
        public string? SpeciesCode { get; set; }
        public string? GearCode { get; set; }
        public string? LifeStage { get; set; }
        public string? MarketCode { get; set; }
        public string? Currency { get; set; }
        public string? CountryCode { get; set; }
        public Measurement? Measurements { get; set; }
    }
}
