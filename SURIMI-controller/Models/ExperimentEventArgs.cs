namespace SURIMI_controller.Models
{
    public class ExperimentEventArgs : EventArgs
    {
        public string SimulationId { get; set; }
        public string ExperimentId { get; set; }
        public DateTime Current { get; set; }
        public CancellationToken Token { get; set; }
        public ExperimentEventArgs(string experimentId, string simulationId, DateTime current, CancellationToken token)
        {
            ExperimentId = experimentId;
            SimulationId = simulationId;
            Current = current;
            Token = token;
        }

        public ExperimentEventArgs(ExperimentEventArgs experimentEventArgs)
        {
            ExperimentId = experimentEventArgs.ExperimentId;
            SimulationId = experimentEventArgs.SimulationId;
            Current = experimentEventArgs.Current;
            Token = experimentEventArgs.Token;
        }
    }
}
