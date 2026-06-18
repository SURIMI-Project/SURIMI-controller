namespace SURIMI_gui.Models
{
    public class ExperimentConfig
    {
        public string? ScenarioName {get;set;} = "western_med";
        public DateTime? EndDateTime { get; set; }
        public int NumberOfRuns { get; set; } = 1;
        public string ExperimentId { get; set; } = "";
    }
}
