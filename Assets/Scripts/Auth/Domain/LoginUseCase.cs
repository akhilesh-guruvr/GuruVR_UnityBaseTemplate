using Cysharp.Threading.Tasks;

namespace Domain
{
    public class LoginUseCase
    {
        private readonly IAuthRepository _authRepository;
        private readonly ITokenStorage _tokenStorage;

        public LoginUseCase(IAuthRepository repo, ITokenStorage storage)
        {
            _authRepository = repo;
            _tokenStorage = storage;
        }

        public async UniTask<(bool success, string message)> ExecuteAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return (false, "Enter username & password");

            var resp = await _authRepository.LoginAsync(username, password);

            if (resp == null)
                return (false, "Network error");

            if (!string.IsNullOrEmpty(resp.access_token))
            {
                _tokenStorage.SaveAccessToken(resp.access_token);
                return (true, resp.message ?? "Login successful");
            }

            return (false, resp.message ?? "Login failed");
        }
    }
}
