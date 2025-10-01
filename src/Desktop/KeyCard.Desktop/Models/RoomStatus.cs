// Models/RoomStatus.cs
namespace KeyCard.Desktop.Models
{
    public enum RoomStatus
    {
        Unknown = 0,

        // Generic states (match mock + backend wording)
        Available,
        Dirty,
        Clean,
        Occupied,
        Vacant,

        // Optional/extra states may already be using
        Inspected,
        OutOfService,
        NeedsMaintenance,

        // If using the "VacantX/OccupiedX" style elsewhere, keep them:
        VacantClean,
        VacantDirty,
        OccupiedClean,
        OccupiedDirty
    }
}
