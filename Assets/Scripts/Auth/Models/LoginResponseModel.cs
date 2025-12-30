namespace Domain.Models
{
    [System.Serializable]
    public class LoginResponseModel
    {
        public string access_token;
        public string refresh_token;
        public string message;
    }
}
