using System.Text.Json.Serialization;

namespace Tracker.Models.Quests
{
    public class QuestObjective
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string? Type { get; set; }
        public string? Tool { get; set; }
        public string? Target { get; set; }
        public int Number { get; set; }
        public List<string> With { get; set; } = new List<string>();
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Map Location { get; set; } = Map.any;
        public int QuestId { get; set; }
        [JsonIgnore]
        public Quest Quest { get; set; } = null!;
    }
}
