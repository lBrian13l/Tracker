using Tracker.Models.Users;

namespace Tracker.Models.Hideout
{
    public class UserStation
    {
        public int Level { get; set; }
        public int UserInfoId { get; set; }
        public UserInfo UserInfo { get; set; } = null!;
        public int StationId { get; set; }
        public Station Station { get; set; } = null!;
    }
}
