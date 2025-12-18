namespace KeyCard.Web.Services;

public class BookingStateService
{
    // Date selection
    public DateTime? CheckInDate { get; set; }
    public DateTime? CheckOutDate { get; set; }
    public int Adults { get; set; } = 1;
    public int Children { get; set; } = 0;

    // Room selection
    public int? SelectedRoomId { get; set; }
    public string? SelectedRoomNumber { get; set; }
    public string? SelectedRoomType { get; set; }
    public decimal PricePerNight { get; set; }
    public decimal TotalAmount { get; set; }

    // Confirmation
    public string? ConfirmationCode { get; set; }
    public int? BookingId { get; set; }

    public int Nights => CheckInDate.HasValue && CheckOutDate.HasValue
        ? Math.Max(1, (CheckOutDate.Value.Date - CheckInDate.Value.Date).Days)
        : 0;

    public void Reset()
    {
        CheckInDate = null;
        CheckOutDate = null;
        Adults = 1;
        Children = 0;
        SelectedRoomId = null;
        SelectedRoomNumber = null;
        SelectedRoomType = null;
        PricePerNight = 0;
        TotalAmount = 0;
        ConfirmationCode = null;
        BookingId = null;
    }

    public void SelectRoom(int roomId, string roomNumber, string roomType, decimal pricePerNight)
    {
        SelectedRoomId = roomId;
        SelectedRoomNumber = roomNumber;
        SelectedRoomType = roomType;
        PricePerNight = pricePerNight;
        TotalAmount = pricePerNight * Nights;
    }
}

