namespace Domain
{
    public interface ITokenStorage
    {
        void SaveAccessToken(string token);
        string GetAccessToken();
        void Clear();
    }
}
