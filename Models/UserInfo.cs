using Tracker.Models.Hideout;
using Tracker.Models.Quests;

namespace Tracker.Models
{
    public class UserInfo
    {
        public int Id { get; set; }
        public int Level { get; set; } = 1;
        public List<Quest> Quests { get; set; } = new();
        public List<Station> Stations { get; set; } = new();
        public List<UserInfoStationCross> StationCrosses { get; set; } = new();
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
