using SURIMI.Datamodel;

namespace SURIMI_controller.ConfigurationService
{
    public interface IConfigurationService
    {
        SurimiConfiguration DeserialiseConfiguration(string yaml);
        Task<SurimiConfiguration> ReadConfigurationAsync(string contractName);
    }
}