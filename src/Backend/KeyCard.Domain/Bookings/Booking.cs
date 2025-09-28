public sealed class Booking
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string ConfirmationCode { get; private set; } = "";
    public string GuestLastName { get; private set; } = "";
    public int RoomNumber { get; private set; }
    public DateOnly CheckInDate { get; private set; }
    public DateOnly CheckOutDate { get; private set; }
    public string Status { get; private set; } = "Reserved";

    private Booking() { }

    public Booking(string code, string lastName, int room, DateOnly inDate, DateOnly outDate)
    {
        ConfirmationCode = code;
        GuestLastName = lastName;
        RoomNumber = room;
        CheckInDate = inDate;
        CheckOutDate = outDate;
    }

    public void CheckIn() => Status = "CheckedIn";
    public void CheckOut() => Status = "CheckedOut";
    public void Cancel() => Status = "Cancelled";
}
