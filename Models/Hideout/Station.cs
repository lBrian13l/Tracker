using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Tracker.Models.Hideout
{
    public class Station
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<Module> Modules { get; set; } = new();
        [JsonIgnore]
        public List<UserInfo> UserInfos { get; set; } = new();
        [JsonIgnore]
        public List<UserInfoStationCross> UserInfoCrosses { get; set; } = new();
    }
}
