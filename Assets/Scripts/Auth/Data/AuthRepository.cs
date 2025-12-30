using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using System.Text;
using UnityEngine;
using Domain;
using Domain.Models;
using System.Collections.Generic;

namespace Data
{
    public enum PostMode { Json, FormUrlEncoded }

    public class AuthRepository : IAuthRepository
    {
        private readonly string _loginUrl;
        private readonly int _timeoutSeconds;
        private readonly ILogger _logger;
        private readonly DebugConfig _debugConfig;
        private readonly PostMode _postMode;
        private const string TAG = "AuthRepository";

        public AuthRepository(string loginUrl, int timeoutSeconds = 15, ILogger logger = null, DebugConfig debugConfig = null, PostMode postMode = PostMode.Json)
        {
            _loginUrl = loginUrl;
            _timeoutSeconds = timeoutSeconds;
            _logger = logger ?? new NullLogger();
            _debugConfig = debugConfig;
            _postMode = postMode;
        }

        public async UniTask<LoginResponseModel> LoginAsync(string username, string password)
        {
            _logger.Info(TAG, $"LoginAsync start for '{username}' (mode={_postMode})");

            UnityWebRequest req = null;
            string bodyToLog = null;

            if (_postMode == PostMode.FormUrlEncoded)
            {
                // Build form-data as x-www-form-urlencoded string to match Swagger
                var kv = new Dictionary<string, string>()
                {
                    ["grant_type"] = "password",
                    ["username"] = username ?? "",
                    ["password"] = password ?? "",
                    ["scope"] = "",
                    ["client_id"] = "",
                    ["client_secret"] = ""
                };

                // create encoded body (same as curl -d)
                var sb = new StringBuilder();
                bool first = true;
                foreach (var pair in kv)
                {
                    if (!first) sb.Append('&');
                    sb.Append(UnityWebRequest.EscapeURL(pair.Key));
                    sb.Append('=');
                    sb.Append(UnityWebRequest.EscapeURL(pair.Value));
                    first = false;
                }
                bodyToLog = sb.ToString();

                var bodyRaw = Encoding.UTF8.GetBytes(bodyToLog);
                req = new UnityWebRequest(_loginUrl, "POST")
                {
                    uploadHandler = new UploadHandlerRaw(bodyRaw),
                    downloadHandler = new DownloadHandlerBuffer()
                };
                req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            }
            else // Json
            {
                var payload = new { username = username, password = password };
                string json = JsonUtility.ToJson(payload);
                bodyToLog = json;

                var bodyRaw = Encoding.UTF8.GetBytes(json);
                req = new UnityWebRequest(_loginUrl, "POST")
                {
                    uploadHandler = new UploadHandlerRaw(bodyRaw),
                    downloadHandler = new DownloadHandlerBuffer()
                };
                req.SetRequestHeader("Content-Type", "application/json");
            }

            req.timeout = _timeoutSeconds;

            // --- Log request details if verbose ---
            if (_debugConfig != null && _debugConfig.verboseLogs)
            {
                _logger.Info(TAG, $"REQUEST -> Method: POST, URL: {_loginUrl}");
                _logger.Info(TAG, $"REQUEST -> Timeout: {req.timeout}s");
                // headers: we can log what we've set
                try
                {
                    // UnityWebRequest.GetRequestHeader is available to read an individual header
                    _logger.Info(TAG, $"REQUEST -> Content-Type: {req.GetRequestHeader("Content-Type")}");
                }
                catch { /* ignore if not available */ }

                // Log body (trimmed)
                _logger.Info(TAG, $"REQUEST -> Body (trimmed): {TrimForLog(bodyToLog, 1000)}");
            }

            UnityWebRequestAsyncOperation op;
            try
            {
                op = req.SendWebRequest();
            }
            catch (System.Exception ex)
            {
                _logger.Error(TAG, "SendWebRequest threw exception", ex);
                return new LoginResponseModel { message = "Network error (request failed)" };
            }

#if UNITY_2020_1_OR_NEWER
            while (!op.isDone) await UniTask.Yield();
            bool networkErr = req.result == UnityWebRequest.Result.ConnectionError;
            bool httpErr = req.result == UnityWebRequest.Result.ProtocolError;
#else
            await op;
            bool networkErr = req.isNetworkError;
            bool httpErr = req.isHttpError;
#endif

            string respText = req.downloadHandler?.text ?? "";

            // If you explicitly want the full raw response, use the toggle
            if (_debugConfig != null && _debugConfig.logRawResponse)
            {
                _logger.Warn(TAG, $"FULL RAW SERVER RESPONSE:\n{respText}");
            }
            else if (_debugConfig != null && _debugConfig.verboseLogs)
            {
                _logger.Info(TAG, $"Raw response text length: {respText.Length}");
            }

            // Log response headers when verbose
            if (_debugConfig != null && _debugConfig.verboseLogs)
            {
                // Unity does not provide a direct dictionary of all response headers in older versions,
                // but UnityWebRequest.GetResponseHeaders() returns a Dictionary<string,string>.
                try
                {
                    var respHeaders = req.GetResponseHeaders();
                    if (respHeaders != null)
                    {
                        foreach (var kvp in respHeaders)
                        {
                            _logger.Info(TAG, $"RESPONSE HEADER: {kvp.Key}: {kvp.Value}");
                        }
                    }
                }
                catch { /* ignore */ }
            }

            if (networkErr || httpErr)
            {
                if (!string.IsNullOrEmpty(respText))
                    _logger.Warn(TAG, $"HTTP error. Response (trimmed): {TrimForLog(respText)}");
                else
                    _logger.Warn(TAG, $"HTTP error: {req.error}");

                var parsedErr = TryParseResponse(respText);
                if (parsedErr != null) return parsedErr;
                return new LoginResponseModel { message = req.error ?? "Network error" };
            }

            var parsed = TryParseResponse(respText);
            if (parsed != null) return parsed;

            _logger.Warn(TAG, "Unable to parse server response as LoginResponseModel.");
            _logger.Info(TAG, $"Server returned (trimmed): {TrimForLog(respText)}");
            return new LoginResponseModel { message = "Invalid server response" };
        }

        private LoginResponseModel TryParseResponse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonUtility.FromJson<LoginResponseModel>(json);
            }
            catch (System.Exception ex)
            {
                _logger.Warn(TAG, $"JsonUtility parse failed: {ex.Message}");
                return null;
            }
        }

        private string TrimForLog(string s, int max = 1000)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length <= max) return s;
            return s.Substring(0, max) + "...(truncated)";
        }
    }
}
