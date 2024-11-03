using System.Text.Json.Serialization;

namespace Tracker.Models.Quests
{
    public class QuestRequirements
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public List<int> Quests { get; set; } = new();
        public int QuestId { get; set; }
        [JsonIgnore]
        public Quest Quest { get; set; } = null!;
    }
}
