namespace XTSApiClient.Models
{
    public class LoginResponse
    {
        public string? Type { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public LoginResult? Result { get; set; }
    }

    public class LoginResult
    {
        public string? Token { get; set; }
        public string? UserID { get; set; }
        public bool IsInvestorClient { get; set; }
    }
}
