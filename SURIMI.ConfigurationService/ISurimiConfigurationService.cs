using SURIMI.ConfigurationService.Models;

namespace SURIMI.ConfigurationService
{
    public interface ISurimiConfigurationService
    {
        SurimiConfiguration ReadYaml(string yaml);
    }
}