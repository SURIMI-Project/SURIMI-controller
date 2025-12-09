namespace SURIMI.ConfigurationService.Models
{
    public class Simulation
    {
        public string? CaseStudyName { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime MaximumEndDateTime { get; set; }
        public string? TimeStep { get; set; }
        public Geography? Geography { get; set; }
        public Standards? Standards { get; set; }
    }
}
