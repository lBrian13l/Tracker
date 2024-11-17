using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Tracker.Models.Quests
{
    public class Quest
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        [JsonPropertyName("require")]
        public QuestRequirements? Requirements { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Trader Giver { get; set; }
        public string? Title { get; set; }
        public string? Wiki { get; set; }
        public List<int> Alternatives { get; set; } = new();
        public List<QuestObjective> Objectives { get; set; } = new();
        [JsonIgnore]
        public List<UserInfo> UserInfos { get; set; } = new();
    }
}
