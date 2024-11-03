using System.Text.Json.Serialization;

namespace Tracker.Models.Hideout
{
    public class Module
    {
        [JsonIgnore]
        public int Id { get; set; }
        [JsonPropertyName("module")]
        public string? Name { get; set; }
        public int Level { get; set; }
        [JsonPropertyName("require")]
        public List<ModuleRequirement> Requirements { get; set; } = new();
        public int StationId { get; set; }
        [JsonIgnore]
        public Station Station { get; set; } = null!;
    }
}
