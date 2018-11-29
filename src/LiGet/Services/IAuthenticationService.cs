using System.Threading.Tasks;

namespace LiGet.Services
{
    public interface IAuthenticationService
    {
        Task<bool> AuthenticateAsync(string apiKey);
    }
}
