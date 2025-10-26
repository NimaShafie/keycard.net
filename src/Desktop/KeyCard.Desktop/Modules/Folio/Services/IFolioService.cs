// Modules/Folio/Services/IFolioService.cs
using System.Collections.Generic;
using System.Threading.Tasks;

using KeyCard.Desktop.Modules.Folio.Models;

namespace KeyCard.Desktop.Modules.Folio.Services
{
    /// <summary>
    /// Service for managing guest folios (financial accounts).
    /// </summary>
    public interface IFolioService
    {
        /// <summary>
        /// Get all active (open) folios.
        /// </summary>
        Task<IReadOnlyList<GuestFolio>> GetActiveFoliosAsync();

        /// <summary>
        /// Get a specific folio by ID.
        /// </summary>
        Task<GuestFolio?> GetFolioByIdAsync(string folioId);

        /// <summary>
        /// Search folios by guest name, booking ID, or room number.
        /// </summary>
        Task<IReadOnlyList<GuestFolio>> SearchFoliosAsync(string searchTerm);

        /// <summary>
        /// Post a charge to a folio.
        /// </summary>
        Task PostChargeAsync(string folioId, decimal amount, string description);

        /// <summary>
        /// Apply a payment to a folio.
        /// </summary>
        Task ApplyPaymentAsync(string folioId, decimal amount, string paymentMethod);

        /// <summary>
        /// Print/generate a folio statement.
        /// </summary>
        Task PrintStatementAsync(string folioId);

        /// <summary>
        /// Close/settle a folio.
        /// </summary>
        Task CloseFolioAsync(string folioId);
    }
}
