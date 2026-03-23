using SURIMI.Datamodel;

namespace SURIMI.ConfigurationService
{
    public interface ISurimiConfigurationService
    {
        SurimiConfiguration DeserialiseConfiguration(string yaml);
        Task<SurimiConfiguration> ReadConfigurationAsync(string contractName);
    }
}