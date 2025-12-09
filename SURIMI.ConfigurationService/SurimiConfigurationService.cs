using SURIMI.ConfigurationService.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SURIMI.ConfigurationService
{
    public class SurimiConfigurationService : ISurimiConfigurationService
    {
        public SurimiConfiguration ReadYaml(string yaml)
        {
            // Implementation for reading YAML configuration
            var input = new StringReader(yaml);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var configuration = deserializer.Deserialize<SurimiConfiguration>(input);

            return configuration;
        }
    }
}
