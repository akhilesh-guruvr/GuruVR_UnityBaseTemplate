using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using System.Threading;
using Presentation.Model;
using Presentation.View;
using Domain;
using Data;

namespace Presentation.Controller
{
    public class LoginController : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private LoginView loginView;

        [Header("Network Settings")]
        [SerializeField] private string loginUrl = "https://chatbackenddev.guruvrmetaversity.com/auth/login";
        [SerializeField] private int timeoutSeconds = 15;

        [Header("Debugging")]
        [SerializeField] private DebugConfig debugConfig; // assign the ScriptableObject created earlier

        [Header("Events")]
        public UnityEvent OnLoginSuccess;

        private LoginModel _model;
        private LoginUseCase _useCase;

        private AuthRepository _repo;
        private PlayerPrefsTokenStorage _storage;

        private CancellationTokenSource _cts;
        private bool _isProcessing;
        private ILogger _logger;

        private void Awake()
        {
            // Create logger based on DebugConfig (defaults to non-verbose)
            _logger = new UnityLogger(debugConfig != null && debugConfig.verboseLogs);

            _model = new LoginModel();
            _repo = new AuthRepository(loginUrl, timeoutSeconds, _logger, debugConfig, PostMode.FormUrlEncoded);
            _storage = new PlayerPrefsTokenStorage();
            _useCase = new LoginUseCase(_repo, _storage);

            _cts = new CancellationTokenSource();

            if (loginView != null) loginView.BindModel(_model);

            _logger.Info("LoginController", $"Awake complete. LoginUrl={loginUrl}");
        }

        private void OnDestroy()
        {
            _cts.Cancel();
            if (loginView != null) loginView.Unbind();
        }

        public void OnLoginButtonPressed()
        {
            if (_isProcessing) return;

            if (loginView == null)
            {
                _logger.Error("LoginController", "loginView is not assigned.");
                Debug.LogError("LoginController: loginView is not assigned.");
                return;
            }

            var (username, password) = loginView.ReadInputs();
            _model.Username = username;
            _model.Password = password;

            PerformLoginAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid PerformLoginAsync(CancellationToken token)
        {
            _isProcessing = true;
            _model.StatusMessage = "";
            _model.IsLoading = true;

            _logger.Info("LoginController", "PerformLoginAsync started.");

            if (string.IsNullOrEmpty(_model.Username) || string.IsNullOrEmpty(_model.Password))
            {
                _logger.Info("LoginController", "Validation failed: empty fields.");
                _model.StatusMessage = "⚠️ Enter username & password";
                _model.IsLoading = false;
                _isProcessing = false;
                return;
            }

            try
            {
                var (success, message) = await _useCase.ExecuteAsync(_model.Username, _model.Password)
                                                       .AttachExternalCancellation(token);

                _logger.Info("LoginController", $"UseCase returned success={success}, message={message}");

                _model.StatusMessage = message;

                _model.IsLoading = false;
                _isProcessing = false;

                if (success)
                {
                    _logger.Info("LoginController", "Login succeeded — invoking OnLoginSuccess.");
                    OnLoginSuccess?.Invoke();
                }
            }
            catch (System.Exception ex)
            {
                _logger.Error("LoginController", "Unhandled exception during login.", ex);
                _model.StatusMessage = "Unexpected error";
                _model.IsLoading = false;
                _isProcessing = false;
            }
        }

        public void Logout()
        {
            _storage.Clear();
            _model.StatusMessage = "Logged out.";
            _model.Username = "";
            _model.Password = "";
            _logger.Info("LoginController", "Logged out and cleared tokens.");
        }
    }
}
