namespace SurimiController.Services
{
    public interface IS3Services
    {
        Task<string> GetYamlFromS3(string contractName);
    }
}