namespace SURIMI_gui.Models
{
    public class ExperimentConfig
    {
        public string? ScenarioName {get;set;} = "northwestern_med";
        public string? ClimateScenario { get; set; }
        public DateTime? EndDateTime { get; set; }= new DateTime(2017, 01, 01);    // not all the way to 2050
        public bool IsMseRun { get; set; } = true;
        public string ExperimentId { get; set; } = "";
    }
}
