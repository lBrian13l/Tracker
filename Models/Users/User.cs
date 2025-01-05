namespace Tracker.Models.Users
{
    public class User
    {
        public User(string name, string email, string password)
        {
            Name = name;
            Email = email;
            Password = password;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } = "user";
        public UserInfo? UserInfo { get; set; } = new();
    }
}
