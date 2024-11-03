namespace Tracker.Models
{
    public class ProblemQuest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? FieldName { get; set; }
        public List<string> Values { get; set; } = new();
    }
}
