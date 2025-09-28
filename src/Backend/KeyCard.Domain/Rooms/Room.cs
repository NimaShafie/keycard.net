using KeyCard.Domain.Common;

namespace KeyCard.Domain.Rooms;

/// <summary>Aggregate for a hotel room.</summary>
public sealed class Room : Entity
{
    public string Number { get; private set; }
    public string Type { get; private set; }
    public RoomState State { get; private set; } = RoomState.Vacant;

    public Room(string number, string type)
    {
        Number = number;
        Type = type;
    }

    public void MarkDirty() => State = RoomState.Dirty;
    public void MarkCleaning() => State = RoomState.Cleaning;
    public void MarkInspected() => State = RoomState.Inspected;
    public void CheckIn() => State = RoomState.Occupied;
    public void CheckOut() => State = RoomState.Vacant;
}
