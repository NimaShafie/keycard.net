// Modules/Folio/Services/MockFolioService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using KeyCard.Desktop.Modules.Folio.Models; // GuestFolio only

using AppModels = KeyCard.Desktop.Models;   // FolioCharge, FolioPayment

namespace KeyCard.Desktop.Modules.Folio.Services
{
    /// <summary>
    /// Mock IFolioService with proper balance recalculation
    /// </summary>
    public class MockFolioService : IFolioService
    {
        private readonly List<GuestFolio> _folios = new();

        // Sidecar lists when model lacks Charges/Payments
        private sealed class Sidecar
        {
            public List<object> Charges { get; } = new();
            public List<object> Payments { get; } = new();
        }
        private readonly ConditionalWeakTable<GuestFolio, Sidecar> _sidecars = new();

        public MockFolioService()
        {
            SeedMockData();
        }

        // ----------------------------
        // Queries
        // ----------------------------

        public Task<List<GuestFolio>> GetAllFoliosAsync()
        {
            // ✅ FIX: Return clones with recalculated balances
            var result = _folios.Select(f => CloneFolioWithBalance(f)).ToList();
            return Task.FromResult(result);
        }

        public Task<GuestFolio?> GetFolioAsync(string folioId)
        {
            var folio = FindByFolioId(folioId);
            if (folio == null) return Task.FromResult<GuestFolio?>(null);

            // ✅ FIX: Return clone with recalculated balance
            var result = CloneFolioWithBalance(folio);
            return Task.FromResult<GuestFolio?>(result);
        }

        public Task<GuestFolio?> GetFolioByIdAsync(string folioId)
            => GetFolioAsync(folioId);

        public Task<IReadOnlyList<GuestFolio>> GetActiveFoliosAsync()
        {
            // ✅ FIX: Return clones with recalculated balances
            var active = _folios
                .Where(IsOpen)
                .Select(f => CloneFolioWithBalance(f))
                .ToList();
            return Task.FromResult<IReadOnlyList<GuestFolio>>(active);
        }

        public Task<IReadOnlyList<GuestFolio>> SearchFoliosAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetActiveFoliosAsync();

            var term = searchTerm.Trim().ToLowerInvariant();

            bool Matches(GuestFolio f)
            {
                var fields = new[]
                {
                    GetString(f, "Id"),
                    GetString(f, "FolioId"),
                    GetString(f, "Number"),
                    GetString(f, "GuestName"),
                    GetString(f, "BookingId"),
                    GetString(f, "RoomNumber"),
                };
                return fields.Any(s => !string.IsNullOrEmpty(s) && s!.ToLowerInvariant().Contains(term));
            }

            // ✅ FIX: Return clones with recalculated balances
            var results = _folios
                .Where(Matches)
                .Select(f => CloneFolioWithBalance(f))
                .ToList();
            return Task.FromResult<IReadOnlyList<GuestFolio>>(results);
        }

        // ----------------------------
        // Mutations
        // ----------------------------

        public Task AddChargeAsync(string folioId, AppModels.FolioCharge charge)
        {
            var folio = FindByFolioId(folioId);
            if (folio is null) return Task.CompletedTask;

            var item = CreateLineItem(
                description: string.IsNullOrWhiteSpace(charge?.Description) ? "Charge" : charge!.Description!,
                amount: Math.Abs(charge?.Amount ?? 0m),
                isPayment: false,
                method: null,
                when: DateTime.Now
            );

            AddToCharges(folio, item);
            // ✅ Balance will be recalculated when GetActiveFoliosAsync is called

            Console.WriteLine($"Added charge ${charge?.Amount} to folio {folioId}. New balance: {GetBalance(folio)}");

            return Task.CompletedTask;
        }

        public Task AddPaymentAsync(string folioId, AppModels.FolioPayment payment)
        {
            var folio = FindByFolioId(folioId);
            if (folio is null) return Task.CompletedTask;

            var method = string.IsNullOrWhiteSpace(payment?.Method) ? null : payment!.Method!;
            var desc = method is null ? "Payment" : $"{method} Payment";

            var item = CreateLineItem(
                description: desc,
                amount: Math.Abs(payment?.Amount ?? 0m),
                isPayment: true,
                method: method,
                when: DateTime.Now
            );

            AddToPayments(folio, item);
            // ✅ Balance will be recalculated when GetActiveFoliosAsync is called

            Console.WriteLine($"Added payment ${payment?.Amount} to folio {folioId}. New balance: {GetBalance(folio)}");

            return Task.CompletedTask;
        }

        public Task RemoveChargeAsync(string folioId, string lineItemId)
        {
            var folio = FindByFolioId(folioId);
            if (folio is null) return Task.CompletedTask;

            RemoveFromListById(GetChargesIList(folio), GetChargesSidecar(folio), lineItemId);
            return Task.CompletedTask;
        }

        public Task RemovePaymentAsync(string folioId, string lineItemId)
        {
            var folio = FindByFolioId(folioId);
            if (folio is null) return Task.CompletedTask;

            RemoveFromListById(GetPaymentsIList(folio), GetPaymentsSidecar(folio), lineItemId);
            return Task.CompletedTask;
        }

        // Legacy helpers
        public Task PostChargeAsync(string folioId, decimal amount, string description)
            => AddChargeAsync(folioId, new AppModels.FolioCharge { Amount = amount, Description = description });

        public Task ApplyPaymentAsync(string folioId, decimal amount, string paymentMethod)
            => AddPaymentAsync(folioId, new AppModels.FolioPayment { Amount = amount, Method = paymentMethod });

        // ----------------------------
        // Statements / Invoice
        // ----------------------------

        public Task PrintStatementAsync(string folioId)
        {
            var folio = FindByFolioId(folioId);
            if (folio is null) return Task.CompletedTask;

            var path = WriteStatementToTemp(folio);
            TryOpen(path);
            return Task.CompletedTask;
        }

        public Task<string> GenerateInvoiceAsync(string folioId)
        {
            var folio = FindByFolioId(folioId);
            if (folio is null) return Task.FromResult(string.Empty);

            var path = WriteStatementToTemp(folio);
            return Task.FromResult(path);
        }

        public Task CloseFolioAsync(string folioId)
        {
            var folio = FindByFolioId(folioId);
            if (folio is null) return Task.CompletedTask;

            SetProp(folio, "Status", "Closed");
            return Task.CompletedTask;
        }

        // ----------------------------
        // ✅ NEW: Clone method to create fresh objects with calculated balances
        // ----------------------------

        private GuestFolio CloneFolioWithBalance(GuestFolio source)
        {
            var charges = EnumerateCharges(source);
            var payments = EnumeratePayments(source);

            decimal totalCharges = charges.Sum(c => GetDecimal(c, "Amount") ?? 0m);
            decimal totalPayments = payments.Sum(p => GetDecimal(p, "Amount") ?? 0m);

            // Create new GuestFolio with calculated totals
            return new GuestFolio
            {
                FolioId = source.FolioId,
                BookingId = source.BookingId,
                GuestName = source.GuestName,
                RoomNumber = source.RoomNumber,
                CheckInDate = source.CheckInDate,
                CheckOutDate = source.CheckOutDate,
                TotalCharges = totalCharges,      // ✅ Explicitly set
                TotalPayments = totalPayments,    // ✅ Explicitly set
                Status = source.Status,
                LineItems = new List<FolioLineItem>(source.LineItems) // Clone the list
            };
        }

        // ----------------------------
        // Balance Recalculation (kept for compatibility)
        // ----------------------------

        private void RecalculateBalance(GuestFolio folio)
        {
            var charges = EnumerateCharges(folio);
            var payments = EnumeratePayments(folio);

            decimal totalCharges = charges.Sum(c => GetDecimal(c, "Amount") ?? 0m);
            decimal totalPayments = payments.Sum(p => GetDecimal(p, "Amount") ?? 0m);
            decimal balance = totalCharges - totalPayments;

            // Try to set TotalCharges, TotalPayments if properties exist
            SetProp(folio, "TotalCharges", totalCharges);
            SetProp(folio, "TotalPayments", totalPayments);

            // Try to set Balance if it's writable (it might be computed property)
            bool balanceSet = SetProp(folio, "Balance", balance);

            Console.WriteLine($"Recalculated folio {GetString(folio, "FolioId")}: Charges={totalCharges:C}, Payments={totalPayments:C}, Balance={balance:C}, BalanceSet={balanceSet}");
        }

        private decimal GetBalance(GuestFolio folio)
        {
            var balance = GetDecimal(folio, "Balance");
            if (balance.HasValue) return balance.Value;

            // Fallback calculation
            var charges = EnumerateCharges(folio).Sum(c => GetDecimal(c, "Amount") ?? 0m);
            var payments = EnumeratePayments(folio).Sum(p => GetDecimal(p, "Amount") ?? 0m);
            return charges - payments;
        }

        // ----------------------------
        // Internals
        // ----------------------------

        private GuestFolio? FindByFolioId(string folioId)
        {
            if (string.IsNullOrWhiteSpace(folioId)) return null;
            var key = folioId.Trim();

            foreach (var f in _folios)
            {
                if (EqualsIgnoreCase(GetString(f, "Id"), key)) return f;
                if (EqualsIgnoreCase(GetString(f, "FolioId"), key)) return f;
                if (EqualsIgnoreCase(GetString(f, "Number"), key)) return f;
            }
            return null;
        }

        private static bool EqualsIgnoreCase(string? a, string b)
            => !string.IsNullOrEmpty(a) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private bool IsOpen(GuestFolio f)
        {
            var statusObj = GetProp(f, "Status");
            if (statusObj is null) return true;
            if (statusObj is string s)
                return string.Equals(s, "Open", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        private object CreateLineItem(string description, decimal amount, bool isPayment, string? method, DateTime when)
        {
            var li = new FolioLineItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Description = description,
                Amount = amount,
                Type = isPayment ? "Payment" : "Charge",
                TransactionDate = when,
                Reference = method
            };

            return li;
        }

        private void AddToCharges(GuestFolio f, object lineItem)
        {
            if (lineItem is FolioLineItem item)
            {
                f.LineItems.Add(item);
                Console.WriteLine($"Added charge line item: {item.Description} ${item.Amount}");
            }
        }

        private void AddToPayments(GuestFolio f, object lineItem)
        {
            if (lineItem is FolioLineItem item)
            {
                f.LineItems.Add(item);
                Console.WriteLine($"Added payment line item: {item.Description} ${item.Amount}");
            }
        }

        private static void RemoveFromListById(System.Collections.IList? ilist, List<object> sidecar, string lineItemId)
        {
            if (!string.IsNullOrWhiteSpace(lineItemId))
            {
                if (ilist is not null)
                {
                    for (int i = 0; i < ilist.Count; i++)
                    {
                        var id = GetString(ilist[i]!, "Id");
                        if (EqualsIgnoreCase(id, lineItemId)) { ilist.RemoveAt(i); return; }
                    }
                }
                else
                {
                    var idx = sidecar.FindIndex(o => EqualsIgnoreCase(GetString(o, "Id"), lineItemId));
                    if (idx >= 0) sidecar.RemoveAt(idx);
                }
            }
        }

        private string WriteStatementToTemp(GuestFolio folio)
        {
            var idForFile = GetString(folio, "FolioId") ?? "UNKNOWN";
            var path = Path.Combine(Path.GetTempPath(), $"Statement_{idForFile}_{DateTime.Now:yyyyMMddHHmmss}.txt");

            var charges = EnumerateCharges(folio).ToList();
            var payments = EnumeratePayments(folio).ToList();

            decimal Sum(IEnumerable<object> items) => items.Sum(o => GetDecimal(o, "Amount") ?? 0m);

            var totalCharges = Sum(charges);
            var totalPayments = Sum(payments);
            var balance = GetBalance(folio);

            var lines = new List<string>
            {
                "====================================",
                "     KEYCARD.NET HOTEL STATEMENT",
                "====================================",
                "",
                $"Folio Number: {idForFile}",
                $"Guest Name:   {GetString(folio, "GuestName") ?? "—"}",
                $"Room Number:  {GetString(folio, "RoomNumber") ?? "—"}",
                $"Booking ID:   {GetString(folio, "BookingId") ?? "—"}",
                $"Statement:    {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "CHARGES:",
                "------------------------------------"
            };

            foreach (var c in charges.OrderBy(x => GetDate(x, "TransactionDate") ?? DateTime.MinValue))
            {
                var when = GetDate(c, "TransactionDate")?.ToString("yyyy-MM-dd") ?? "";
                var desc = GetString(c, "Description") ?? "—";
                var amt = (GetDecimal(c, "Amount") ?? 0m).ToString("0.00");
                lines.Add($"{when}  {desc,-30} ${amt,10}");
            }

            lines.AddRange(new[]
            {
                "",
                "PAYMENTS:",
                "------------------------------------"
            });

            foreach (var p in payments.OrderBy(x => GetDate(x, "TransactionDate") ?? DateTime.MinValue))
            {
                var when = GetDate(p, "TransactionDate")?.ToString("yyyy-MM-dd") ?? "";
                var desc = GetString(p, "Description") ?? "—";
                var amt = (GetDecimal(p, "Amount") ?? 0m).ToString("0.00");
                lines.Add($"{when}  {desc,-30} ${amt,10}");
            }

            lines.AddRange(new[]
            {
                "",
                "====================================",
                $"Total Charges:          ${totalCharges,10:0.00}",
                $"Total Payments:         ${totalPayments,10:0.00}",
                "------------------------------------",
                $"BALANCE DUE:            ${balance,10:0.00}",
                "====================================",
                "",
                $"Status: {GetString(folio, "Status") ?? "—"}",
                "",
                "Thank you for your stay!"
            });

            File.WriteAllLines(path, lines);
            return path;
        }

        private static void TryOpen(string path)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
            catch { /* ignore */ }
        }

        // ----------------------------
        // Seeding
        // ----------------------------

        private void SeedMockData()
        {
            _folios.Add(CreateFolio(
                folioId: "F001",
                guestName: "John Smith",
                room: "101",
                booking: "BK001",
                status: "Open",
                initialCharges: new[]
                {
                    ("Room Charge (3 nights)", 450.00m),
                    ("Mini Bar", 25.00m),
                    ("Room Service", 75.00m),
                },
                initialPayments: new[]
                {
                    ("Credit Card Payment", 300.00m, "Credit Card"),
                }
            ));

            _folios.Add(CreateFolio(
                folioId: "F002",
                guestName: "Jane Doe",
                room: "202",
                booking: "BK002",
                status: "Open",
                initialCharges: new[]
                {
                    ("Room Charge (2 nights)", 300.00m),
                },
                initialPayments: Array.Empty<(string, decimal, string?)>()
            ));

            _folios.Add(CreateFolio(
                folioId: "F003",
                guestName: "Bob Wilson",
                room: "305",
                booking: "BK003",
                status: "Open",
                initialCharges: new[]
                {
                    ("Room Charge (1 night)", 150.00m),
                },
                initialPayments: new[]
                {
                    ("Cash Payment", 150.00m, "Cash"),
                }
            ));
        }

        private GuestFolio CreateFolio(
            string folioId,
            string guestName,
            string room,
            string booking,
            string status,
            (string desc, decimal amount)[] initialCharges,
            (string desc, decimal amount, string? method)[] initialPayments)
        {
            var f = new GuestFolio
            {
                FolioId = folioId,
                GuestName = guestName,
                RoomNumber = int.TryParse(room, out var rn) ? rn : 0,
                BookingId = booking,
                Status = status,
                CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-2)),
                CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                LineItems = new List<FolioLineItem>()
            };

            foreach (var (desc, amt) in initialCharges)
            {
                var item = CreateLineItem(desc, amt, isPayment: false, method: null, when: DateTime.Now);
                AddToCharges(f, item);
            }

            foreach (var (desc, amt, method) in initialPayments)
            {
                var item = CreateLineItem(desc, amt, isPayment: true, method: method, when: DateTime.Now);
                AddToPayments(f, item);
            }

            RecalculateBalance(f);
            return f;
        }

        // ----------------------------
        // Reflection helpers
        // ----------------------------

        private static object? GetProp(object obj, string name)
            => obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(obj);

        private static bool SetProp(object obj, string name, object? value)
        {
            var pi = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (pi is null || !pi.CanWrite) return false;

            try
            {
                var targetType = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;

                if (value is null)
                {
                    if (!targetType.IsValueType || Nullable.GetUnderlyingType(pi.PropertyType) != null)
                    {
                        pi.SetValue(obj, null);
                        return true;
                    }
                    return false;
                }

                if (targetType.IsAssignableFrom(value.GetType()))
                {
                    pi.SetValue(obj, value);
                    return true;
                }

                var converted = Convert.ChangeType(value, targetType);
                pi.SetValue(obj, converted);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string? GetString(object obj, string name)
            => GetProp(obj, name)?.ToString();

        private static decimal? GetDecimal(object obj, string name)
        {
            var v = GetProp(obj, name);
            if (v is null) return null;
            try { return Convert.ToDecimal(v); } catch { return null; }
        }

        private static DateTime? GetDate(object obj, string name)
        {
            var v = GetProp(obj, name);
            if (v is null) return null;
            if (v is DateTime dt) return dt;
            if (DateTime.TryParse(v.ToString(), out var parsed)) return parsed;
            return null;
        }

        private static System.Collections.IList? GetIList(object obj, string name)
            => obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(obj) as System.Collections.IList;

        private System.Collections.IList? GetChargesIList(GuestFolio f) => f.LineItems;
        private System.Collections.IList? GetPaymentsIList(GuestFolio f) => f.LineItems;

        private List<object> GetChargesSidecar(GuestFolio f) => _sidecars.GetOrCreateValue(f).Charges;
        private List<object> GetPaymentsSidecar(GuestFolio f) => _sidecars.GetOrCreateValue(f).Payments;

        private IEnumerable<object> EnumerateCharges(GuestFolio f)
        {
            return f.LineItems.Where(item => item.Type?.ToLower() == "charge");
        }

        private IEnumerable<object> EnumeratePayments(GuestFolio f)
        {
            return f.LineItems.Where(item => item.Type?.ToLower() == "payment");
        }
    }
}
