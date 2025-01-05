namespace Tracker.Models.Items
{
    public class ItemDto
    {
        public string Id { get; set; } = null!;
        public string? ShortName { get; set; }
        public RelateType RelateType { get; set; }
        public int RelatedId { get; set; }
        public string? RelatedName { get; set; }
        public int Quantity { get; set; }
        public int RequiredQuantity { get; set; }
        public bool FoundInRaid { get; set; }
    }
}
