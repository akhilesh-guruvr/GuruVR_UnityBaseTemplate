using Cysharp.Threading.Tasks;
using Domain.Models;

namespace Domain
{
    public interface IAuthRepository
    {
        UniTask<LoginResponseModel> LoginAsync(string username, string password);
    }
}
