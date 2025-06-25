namespace SurimiController.Models
{
    public class Simulation
    {
        public DateTime SimulationCreated { get; set; }
        public required string ScenarioId { get; set; }
        public required DateTime StartDateTime { get; set; }
        public required string StepSize { get; set; }
        public required string Status { get; set; }
        public DateTime SimulationCurrent { get; set; }
        public required string EcologyHost { get; set; }
    }
}
