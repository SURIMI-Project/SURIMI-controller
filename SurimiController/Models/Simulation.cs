namespace SurimiController.Models
{
    public class Simulation
    {
        public int Order { get; set; }
        public DateTime SimulationStarted { get; set; }
        public TimeSpan SimulationDuration { get; set; }
        public required string ScenarioId { get; set; }
        public required DateTime StartDateTime { get; set; }
        public required string StepSize { get; set; }
        public required DateTime EndDateTime { get; set; }
        public required string Status { get; set; }
        public DateTime SimulationCurrent { get; set; }
        public required string EcologyHost { get; set; }
        public Task? Task { get; set; }
        public CancellationTokenSource? Cts { get; set; }

    }
}
