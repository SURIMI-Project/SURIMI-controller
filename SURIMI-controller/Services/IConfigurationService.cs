using SURIMI.Datamodel;

namespace SURIMI_controller.ConfigurationService
{
    public interface IConfigurationService
    {
        SurimiContract DeserialiseContract(string yaml);
        Task<IEnumerable<string>> GetScenarioNames(CancellationToken cancellationToken);
        Task<SurimiContract> ReadConfigurationAsync(string scenarioName, CancellationToken cancellationToken);
    }
}