
namespace SurimiController.Services
{
    public interface IOAuth2Services
    {
        Task<string> GetAccessTokenAsync(string? username, string? password);
    }
}