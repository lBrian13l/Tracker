using System.Text.Json.Serialization;

namespace Tracker.Models.Hideout
{
    public class ModuleRequirement
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string? Type { get; set; }
        public string? Name { get; set; }
        public int Quantity { get; set; }
        public int ModuleId { get; set; }
        [JsonIgnore]
        public Module Module { get; set; } = null!;
    }
}
