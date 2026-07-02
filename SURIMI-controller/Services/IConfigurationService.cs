using SURIMI.Datamodel;

namespace SURIMI_controller.ConfigurationService
{
    public interface IConfigurationService
    {
        SurimiContract DeserialiseContract(string yaml);
        Task<SurimiContract> ReadConfigurationAsync(string scenarioName);
    }
}