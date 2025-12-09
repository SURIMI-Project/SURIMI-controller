namespace SurimiGUI.Models
{
    public class SimulationStatus
    {
        public required string SimulationId { get; set; }
        public DateTime? SimulationCreated { get; set; }
        public required string ScenarioId { get; set; }
        public required DateTime StartDateTime { get; set; }
        public required DateTime EndDateTime { get; set; }
        public required string Status { get; set; }
        public DateTime? SimulationCurrent { get; set; }
        public string IP { get; set; } = string.Empty;
    }
}
