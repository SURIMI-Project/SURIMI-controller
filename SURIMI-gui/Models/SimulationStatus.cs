namespace SurimiGUI.Models
{
    public class SimulationStatus
    {
        public required string ExperimentId { get; set; }
        public required string SimulationId { get; set; }
        public DateTime? SimulationStarted { get; set; }
        public TimeSpan? SimulationDuration { get; set; }
        public required string ScenarioId { get; set; }
        public required DateTime StartDateTime { get; set; }
        public required DateTime EndDateTime { get; set; }
        public required string Status { get; set; }
        public DateTime? SimulationCurrent { get; set; }
        public string IP { get; set; } = string.Empty;
    }
}
