namespace SteakawayRestaurant.Models
{
    public class MenuItem
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public bool IsAvailable { get; set; }
    }
}