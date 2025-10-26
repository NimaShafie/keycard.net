// Modules/Folio/Services/LiveFolioService.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using KeyCard.Desktop.Modules.Folio.Models;

namespace KeyCard.Desktop.Modules.Folio.Services
{
    /// <summary>
    /// Live API implementation of IFolioService.
    /// Returns empty/stub data until backend endpoints are ready.
    /// </summary>
    public sealed class LiveFolioService : IFolioService
    {
        private readonly HttpClient _http;

        public LiveFolioService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<IReadOnlyList<GuestFolio>> GetActiveFoliosAsync()
        {
            // TODO: Implement when endpoint is ready
            // return await _http.GetFromJsonAsync<List<GuestFolio>>("/api/folio/active") 
            //     ?? new List<GuestFolio>();

            await Task.Delay(100); // Simulate network call
            return Array.Empty<GuestFolio>();
        }

        public async Task<GuestFolio?> GetFolioByIdAsync(string folioId)
        {
            // TODO: Implement when endpoint is ready
            // return await _http.GetFromJsonAsync<GuestFolio>($"/api/folio/{folioId}");

            await Task.Delay(100);
            return null;
        }

        public async Task<IReadOnlyList<GuestFolio>> SearchFoliosAsync(string searchTerm)
        {
            // TODO: Implement when endpoint is ready
            // var encoded = Uri.EscapeDataString(searchTerm);
            // return await _http.GetFromJsonAsync<List<GuestFolio>>($"/api/folio/search?q={encoded}") 
            //     ?? new List<GuestFolio>();

            await Task.Delay(100);
            return Array.Empty<GuestFolio>();
        }

        public async Task PostChargeAsync(string folioId, decimal amount, string description)
        {
            // TODO: Implement when endpoint is ready
            // await _http.PostAsJsonAsync($"/api/folio/{folioId}/charges", new
            // {
            //     amount,
            //     description
            // });

            await Task.Delay(100);
            throw new NotSupportedException("Folio charges not yet available on backend");
        }

        public async Task ApplyPaymentAsync(string folioId, decimal amount, string paymentMethod)
        {
            // TODO: Implement when endpoint is ready
            // await _http.PostAsJsonAsync($"/api/folio/{folioId}/payments", new
            // {
            //     amount,
            //     paymentMethod
            // });

            await Task.Delay(100);
            throw new NotSupportedException("Folio payments not yet available on backend");
        }

        public async Task PrintStatementAsync(string folioId)
        {
            // TODO: Implement when endpoint is ready
            // var response = await _http.GetAsync($"/api/folio/{folioId}/statement");
            // response.EnsureSuccessStatusCode();
            // var pdfBytes = await response.Content.ReadAsByteArrayAsync();
            // // Save or display PDF

            await Task.Delay(100);
            throw new NotSupportedException("Statement printing not yet available on backend");
        }

        public async Task CloseFolioAsync(string folioId)
        {
            // TODO: Implement when endpoint is ready
            // await _http.PostAsync($"/api/folio/{folioId}/close", null);

            await Task.Delay(100);
            throw new NotSupportedException("Close folio not yet available on backend");
        }
    }
}
