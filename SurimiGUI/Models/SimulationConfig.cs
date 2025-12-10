namespace SurimiGUI.Models
{
    public class SimulationConfig
    {
        public string? ScenarioId {get;set;} = "";
        public DateTime StartDateTime { get; set; } = DateTime.UtcNow.AddYears(-20);
        public string StepSize { get; set; } = "P1M"; // 1 month
        public DateTime EndDateTime { get; set; } = DateTime.UtcNow;
        public string SimulationId { get; set; } = "";

    }
}
