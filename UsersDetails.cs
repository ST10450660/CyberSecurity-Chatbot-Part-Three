// UserDetails.cs
namespace CyberSecurity_Chatbot_Part_Three
{
    /// <summary>
    /// Represents the user's persisted details (name and favorite topic).
    /// </summary>
    public class UserDetails
    {
        public string UserName { get; set; } = string.Empty;
        public string FavoriteTopic { get; set; } = string.Empty;
    }
}