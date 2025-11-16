// Modules/Folio/Services/LiveFolioService.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using ModuleModels = KeyCard.Desktop.Modules.Folio.Models; // GuestFolio
using AppModels = KeyCard.Desktop.Models;                  // FolioCharge, FolioPayment

namespace KeyCard.Desktop.Modules.Folio.Services
{
    /// <summary>
    /// Live API implementation of IFolioService.
    /// Currently stubbed; replace throws with real HTTP calls when backend endpoints are ready.
    /// </summary>
    public sealed class LiveFolioService : IFolioService
    {
        private readonly HttpClient _http;

        public LiveFolioService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        // -------- Queries --------

        public Task<List<ModuleModels.GuestFolio>> GetAllFoliosAsync()
            => Task.FromResult(new List<ModuleModels.GuestFolio>());

        public Task<ModuleModels.GuestFolio?> GetFolioAsync(string folioId)
            => Task.FromResult<ModuleModels.GuestFolio?>(null);

        // Back-compat alias required by IFolioService
        public Task<ModuleModels.GuestFolio?> GetFolioByIdAsync(string folioId)
            => GetFolioAsync(folioId);

        public Task<IReadOnlyList<ModuleModels.GuestFolio>> GetActiveFoliosAsync()
            => Task.FromResult<IReadOnlyList<ModuleModels.GuestFolio>>(Array.Empty<ModuleModels.GuestFolio>());

        public Task<IReadOnlyList<ModuleModels.GuestFolio>> SearchFoliosAsync(string searchTerm)
            => Task.FromResult<IReadOnlyList<ModuleModels.GuestFolio>>(Array.Empty<ModuleModels.GuestFolio>());

        // -------- Mutations --------

        public Task AddChargeAsync(string folioId, AppModels.FolioCharge charge)
            => throw new NotSupportedException("AddCharge is not yet implemented on the backend.");

        public Task AddPaymentAsync(string folioId, AppModels.FolioPayment payment)
            => throw new NotSupportedException("AddPayment is not yet implemented on the backend.");

        public Task RemoveChargeAsync(string folioId, string lineItemId)
            => throw new NotSupportedException("RemoveCharge is not yet implemented on the backend.");

        public Task RemovePaymentAsync(string folioId, string lineItemId)
            => throw new NotSupportedException("RemovePayment is not yet implemented on the backend.");

        // Legacy helpers (kept for callers still using them)
        public Task PostChargeAsync(string folioId, decimal amount, string description)
            => throw new NotSupportedException("PostCharge is not yet implemented on the backend.");

        public Task ApplyPaymentAsync(string folioId, decimal amount, string paymentMethod)
            => throw new NotSupportedException("ApplyPayment is not yet implemented on the backend.");

        // -------- Statements / Invoice --------

        public Task PrintStatementAsync(string folioId)
            => throw new NotSupportedException("PrintStatement is not yet implemented on the backend.");

        public Task<string> GenerateInvoiceAsync(string folioId)
            => throw new NotSupportedException("GenerateInvoice is not yet implemented on the backend.");

        public Task CloseFolioAsync(string folioId)
            => throw new NotSupportedException("CloseFolio is not yet implemented on the backend.");
    }
}
