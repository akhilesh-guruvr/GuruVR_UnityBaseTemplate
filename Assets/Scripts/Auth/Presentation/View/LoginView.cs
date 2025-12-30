using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Presentation.Model;

namespace Presentation.View
{
    public class LoginView : MonoBehaviour
    {
        [Header("Inputs")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("Button / Spinner")]
        [SerializeField] private Button loginButton;
        [SerializeField] private TextMeshProUGUI loginButtonLabel;
        [SerializeField] private GameObject loadingSpinner;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusText;

        private LoginModel _model;

        public void BindModel(LoginModel model)
        {
            Unbind();
            _model = model;
            if (_model == null) return;

            usernameInput.text = _model.Username ?? "";
            passwordInput.text = _model.Password ?? "";
            statusText.text = _model.StatusMessage ?? "";
            UpdateLoading(_model.IsLoading);

            _model.OnLoadingChanged += UpdateLoading;
            _model.OnStatusMessageChanged += UpdateStatus;
        }

        public void Unbind()
        {
            if (_model != null)
            {
                _model.OnLoadingChanged -= UpdateLoading;
                _model.OnStatusMessageChanged -= UpdateStatus;
            }

            _model = null;
        }

        public (string username, string password) ReadInputs()
        {
            return (
                usernameInput != null ? usernameInput.text.Trim() : "",
                passwordInput != null ? passwordInput.text : ""
            );
        }

        private void UpdateLoading(bool isLoading)
        {
            if (loadingSpinner != null) loadingSpinner.SetActive(isLoading);
            if (loginButtonLabel != null) loginButtonLabel.gameObject.SetActive(!isLoading);
            if (loginButton != null) loginButton.interactable = !isLoading;
        }

        private void UpdateStatus(string text)
        {
            if (statusText != null)
                statusText.text = text ?? "";
        }
    }
}
