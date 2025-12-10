using SURIMI.Datamodel;

namespace SURIMI.ConfigurationService
{
    public interface ISurimiConfigurationService
    {
        SurimiConfiguration ReadYaml(string yaml);
    }
}