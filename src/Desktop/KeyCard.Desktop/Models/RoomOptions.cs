// Models/RoomOption.cs
namespace KeyCard.Desktop.Models
{
    public sealed class RoomOption
    {
        public int Number { get; set; }
        public string Type { get; set; } = "Regular Room"; // "Regular Room" | "King Room" | "Luxury Room"
    }
}
