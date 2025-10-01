// Models/Room.cs
namespace KeyCard.Desktop.Models;

public sealed record Room
{
    public int Number { get; init; }
    public string Status { get; init; } = "Dirty";
}
