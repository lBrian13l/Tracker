using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Tracker.Models.Users;

namespace Tracker.Models.Items
{
    public class Item
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public string? ShortName { get; set; }
        [JsonIgnore]
        public List<UserInfo> UserInfos { get; set; } = new();
        [JsonIgnore]
        public List<UserItem> UserItems { get; set; } = new();
    }
}
