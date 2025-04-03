namespace SurimiGUI.Models
{
    public class SimulationConfig
    {
        public string? ScenarioId {get;set;} = "";
        public DateTime StartDateTime { get; set; } = DateTime.UtcNow.AddYears(-20);
        public string StepSize { get; set; } = "P1M"; // 1 month
        public string SimulationDuration { get; set; } = "P2Y"; // 1 month
        public string SimulationId { get; set; } = "";

    }
}
