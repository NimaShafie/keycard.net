// Modules/Folio/Services/IFolioService.cs
using System.Collections.Generic;
using System.Threading.Tasks;

using KeyCard.Desktop.Modules.Folio.Models; // GuestFolio

using AppModels = KeyCard.Desktop.Models;   // FolioCharge, FolioPayment

namespace KeyCard.Desktop.Modules.Folio.Services
{
    public interface IFolioService
    {
        Task<List<GuestFolio>> GetAllFoliosAsync();
        Task<GuestFolio?> GetFolioAsync(string folioId);

        // Back-compat for callers (e.g., FolioViewModel) still using the older name
        Task<GuestFolio?> GetFolioByIdAsync(string folioId);

        Task<IReadOnlyList<GuestFolio>> GetActiveFoliosAsync();
        Task<IReadOnlyList<GuestFolio>> SearchFoliosAsync(string searchTerm);

        Task AddChargeAsync(string folioId, AppModels.FolioCharge charge);
        Task AddPaymentAsync(string folioId, AppModels.FolioPayment payment);
        Task RemoveChargeAsync(string folioId, string lineItemId);
        Task RemovePaymentAsync(string folioId, string lineItemId);

        // Legacy helpers preserved for older calling code
        Task PostChargeAsync(string folioId, decimal amount, string description);
        Task ApplyPaymentAsync(string folioId, decimal amount, string paymentMethod);

        Task PrintStatementAsync(string folioId);
        Task<string> GenerateInvoiceAsync(string folioId);
        Task CloseFolioAsync(string folioId);
    }
}
