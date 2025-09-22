namespace KeyCard.Domain.Rooms;

/// <summary>Simple state enum; we’ll evolve into a State pattern later.</summary>
public enum RoomState
{
    Vacant,
    Occupied,
    Dirty,
    Cleaning,
    Inspected
}
