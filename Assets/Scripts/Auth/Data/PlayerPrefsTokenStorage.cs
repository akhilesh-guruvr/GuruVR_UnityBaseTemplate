using UnityEngine;
using Domain;

namespace Data
{
    public class PlayerPrefsTokenStorage : ITokenStorage
    {
        private const string KEY_ACCESS = "AccessToken";
        private const string KEY_LOGIN_FLAG = "LogIn";

        public void SaveAccessToken(string token)
        {
            PlayerPrefs.SetString(KEY_ACCESS, token ?? "");
            PlayerPrefs.SetInt(KEY_LOGIN_FLAG, string.IsNullOrEmpty(token) ? 0 : 1);
            PlayerPrefs.Save();
        }

        public string GetAccessToken() => PlayerPrefs.GetString(KEY_ACCESS, "");

        public void Clear()
        {
            PlayerPrefs.DeleteKey(KEY_ACCESS);
            PlayerPrefs.SetInt(KEY_LOGIN_FLAG, 0);
            PlayerPrefs.Save();
        }
    }
}
