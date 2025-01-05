using System.Text.Json.Serialization;
using Tracker.Models.Hideout;
using Tracker.Models.Items;
using Tracker.Models.Quests;

namespace Tracker.Models.Users
{
    public class UserInfo
    {
        public int Id { get; set; }
        public int Level { get; set; } = 1;
        public List<Quest> Quests { get; set; } = new();
        public List<Station> Stations { get; set; } = new();
        public List<UserStation> UserStations { get; set; } = new();
        public List<Item> Items { get; set; } = new();
        public List<UserItem> UserItems { get; set; } = new();
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
