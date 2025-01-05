using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Tracker.Models.Users;

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
        public List<UserStation> UserStations { get; set; } = new();
    }
}
