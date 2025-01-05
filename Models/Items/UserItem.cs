using Tracker.Models.Users;

namespace Tracker.Models.Items
{
    public class UserItem
    {
        public RelateType RelateType { get; set; }
        public int RelatedId { get; set; }
        public int Quantity { get; set; }
        public int UserInfoId { get; set; }
        public UserInfo UserInfo { get; set; } = null!;
        public string ItemId { get; set; } = null!;
        public Item Item { get; set; } = null!;
    }
}
