// Models/Room.cs
namespace KeyCard.Desktop.Models;

public enum RoomStatus { Available, Occupied, Dirty, OutOfService }
public record Room(int Number, string Type, RoomStatus Status);
