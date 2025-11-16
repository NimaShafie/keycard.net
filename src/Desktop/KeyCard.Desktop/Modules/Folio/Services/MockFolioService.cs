// Modules/Folio/Services/MockFolioService.cs
using System;
using System.Collections;
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
    /// Mock IFolioService that tolerates varying model shapes using reflection.
    /// - No compile-time dependency on LineItemType/FolioStatus/DateOnly, etc.
    /// - If Charges/Payments don't exist on GuestFolio, uses a sidecar store.
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

        // Try to locate optional module types at runtime
        private static readonly Type? FolioLineItemType =
            Type.GetType("KeyCard.Desktop.Modules.Folio.Models.FolioLineItem, KeyCard.Desktop");
        private static readonly Type? LineItemTypeEnum =
            Type.GetType("KeyCard.Desktop.Modules.Folio.Models.LineItemType, KeyCard.Desktop");
        private static readonly Type? FolioStatusEnum =
            Type.GetType("KeyCard.Desktop.Modules.Folio.Models.FolioStatus, KeyCard.Desktop");

        public MockFolioService()
        {
            SeedMockData();
        }

        // ----------------------------
        // Queries
        // ----------------------------

        public Task<List<GuestFolio>> GetAllFoliosAsync()
            => Task.FromResult(_folios.ToList());

        public Task<GuestFolio?> GetFolioAsync(string folioId)
            => Task.FromResult(FindByFolioId(folioId));

        // Back-compat for older callers
        public Task<GuestFolio?> GetFolioByIdAsync(string folioId)
            => GetFolioAsync(folioId);

        public Task<IReadOnlyList<GuestFolio>> GetActiveFoliosAsync()
        {
            var active = _folios.Where(IsOpen).ToList();
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

            var results = _folios.Where(Matches).ToList();
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
            return Task.CompletedTask;
        }

        public Task AddPaymentAsync(string folioId, AppModels.FolioPayment payment)
        {
            var folio = FindByFolioId(folioId);
            if (folio is null) return Task.CompletedTask;

            var method = string.IsNullOrWhiteSpace(payment?.Method) ? null : payment!.Method!;
            // Do NOT rely on payment.Description (not present in your DTO)
            var desc = method is null ? "Payment" : $"{method} Payment";

            var item = CreateLineItem(
                description: desc,
                amount: Math.Abs(payment?.Amount ?? 0m),
                isPayment: true,
                method: method,
                when: DateTime.Now
            );

            AddToPayments(folio, item);
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

            if (FolioStatusEnum is not null)
            {
                var closed = EnumTryParse(FolioStatusEnum, "Closed");
                SetProp(folio, "Status", closed ?? GetProp(folio, "Status"));
            }
            else
            {
                SetProp(folio, "Status", "Closed");
            }

            return Task.CompletedTask;
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

            if (FolioStatusEnum is not null && statusObj.GetType() == FolioStatusEnum)
                return string.Equals(statusObj.ToString(), "Open", StringComparison.OrdinalIgnoreCase);

            if (statusObj is string s)
                return string.Equals(s, "Open", StringComparison.OrdinalIgnoreCase);

            return true;
        }

        private object CreateLineItem(string description, decimal amount, bool isPayment, string? method, DateTime when)
        {
            var li = FolioLineItemType is not null
                ? Activator.CreateInstance(FolioLineItemType)!
                : new object();

            SetProp(li, "Description", description);
            SetProp(li, "Amount", amount);
            SetProp(li, "PaymentMethod", method);

            if (LineItemTypeEnum is not null)
            {
                var val = EnumTryParse(LineItemTypeEnum, isPayment ? "Payment" : "Charge");
                SetProp(li, "Type", val ?? GetProp(li, "Type"));
            }
            else
            {
                SetProp(li, "Type", isPayment ? "Payment" : "Charge");
            }

            // Set any of Date/OccurredAt/Timestamp
            if (!SetProp(li, "Date", when))
                if (!SetProp(li, "OccurredAt", when))
                    SetProp(li, "Timestamp", when);

            // Ensure an Id for removal
            if (string.IsNullOrWhiteSpace(GetString(li, "Id")))
                SetProp(li, "Id", Guid.NewGuid().ToString("N"));

            return li;
        }

        private void AddToCharges(GuestFolio f, object lineItem)
        {
            var ilist = GetChargesIList(f);
            if (ilist is not null) { ilist.Add(lineItem); return; }
            GetChargesSidecar(f).Add(lineItem);
        }

        private void AddToPayments(GuestFolio f, object lineItem)
        {
            var ilist = GetPaymentsIList(f);
            if (ilist is not null) { ilist.Add(lineItem); return; }
            GetPaymentsSidecar(f).Add(lineItem);
        }

        private static void RemoveFromListById(IList? ilist, List<object> sidecar, string lineItemId)
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
            var idForFile = GetString(folio, "Id") ?? GetString(folio, "FolioId") ?? "UNKNOWN";
            var path = Path.Combine(Path.GetTempPath(), $"Statement_{idForFile}_{DateTime.Now:yyyyMMddHHmmss}.txt");

            var charges = EnumerateCharges(folio).ToList();
            var payments = EnumeratePayments(folio).ToList();

            decimal Sum(IEnumerable<object> items) => items.Sum(o => GetDecimal(o, "Amount") ?? 0m);

            var totalCharges = Sum(charges);
            var totalPayments = Sum(payments);

            // Prefer model Balance if readable; otherwise compute
            var balance = GetDecimal(folio, "Balance") ?? (totalCharges - totalPayments);

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
                $"Check-In:     {FormatDate(GetDate(folio, "CheckInDate"))}",
                $"Check-Out:    {FormatDate(GetDate(folio, "CheckOutDate"))}",
                $"Statement:    {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "CHARGES:",
                "------------------------------------"
            };

            foreach (var c in charges.OrderBy(x => GetDate(x, "Date") ?? GetDate(x, "OccurredAt") ?? GetDate(x, "Timestamp") ?? DateTime.MinValue))
            {
                var when = GetDate(c, "Date") ?? GetDate(c, "OccurredAt") ?? GetDate(c, "Timestamp");
                var whenStr = when?.ToString("yyyy-MM-dd") ?? "";
                var desc = GetString(c, "Description") ?? "—";
                var amt = (GetDecimal(c, "Amount") ?? 0m).ToString("0.00");
                lines.Add($"{whenStr}  {desc,-30} ${amt,10}");
            }

            lines.AddRange(new[]
            {
                "",
                "PAYMENTS:",
                "------------------------------------"
            });

            foreach (var p in payments.OrderBy(x => GetDate(x, "Date") ?? GetDate(x, "OccurredAt") ?? GetDate(x, "Timestamp") ?? DateTime.MinValue))
            {
                var when = GetDate(p, "Date") ?? GetDate(p, "OccurredAt") ?? GetDate(p, "Timestamp");
                var whenStr = when?.ToString("yyyy-MM-dd") ?? "";
                var desc = GetString(p, "Description") ?? "—";
                var amt = (GetDecimal(p, "Amount") ?? 0m).ToString("0.00");
                lines.Add($"{whenStr}  {desc,-30} ${amt,10}");
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
                $"Status: {GetString(folio, "Status") ?? GetProp(folio, "Status")?.ToString() ?? "—"}",
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
                checkInOffsetDays: -2,
                checkOutOffsetDays: 1,
                status: "Open",
                initialCharges: new[]
                {
                    ("Room Charge (3 nights)", 450.00m, -2),
                    ("Mini Bar", 25.00m, -1),
                    ("Room Service", 75.00m, 0),
                },
                initialPayments: new[]
                {
                    ("Credit Card Payment", 300.00m, -1, "Credit Card"),
                }
            ));

            _folios.Add(CreateFolio(
                folioId: "F002",
                guestName: "Jane Doe",
                room: "202",
                booking: "BK002",
                checkInOffsetDays: -1,
                checkOutOffsetDays: 2,
                status: "Open",
                initialCharges: new[]
                {
                    ("Room Charge (2 nights)", 300.00m, -1),
                },
                initialPayments: Array.Empty<(string, decimal, int, string?)>()
            ));

            _folios.Add(CreateFolio(
                folioId: "F003",
                guestName: "Bob Wilson",
                room: "305",
                booking: "BK003",
                checkInOffsetDays: 0,
                checkOutOffsetDays: 1,
                status: "Open",
                initialCharges: new[]
                {
                    ("Room Charge (1 night)", 150.00m, 0),
                },
                initialPayments: new[]
                {
                    ("Cash Payment", 150.00m, 0, "Cash"),
                }
            ));
        }

        private GuestFolio CreateFolio(
            string folioId,
            string guestName,
            string room,
            string booking,
            int checkInOffsetDays,
            int checkOutOffsetDays,
            string status,
            (string desc, decimal amount, int dayOffset)[] initialCharges,
            (string desc, decimal amount, int dayOffset, string? method)[] initialPayments)
        {
            var f = Activator.CreateInstance<GuestFolio>();

            // Id/FolioId
            if (!SetProp(f, "Id", folioId))
                SetProp(f, "FolioId", folioId);

            SetProp(f, "GuestName", guestName);
            SetProp(f, "RoomNumber", room);
            SetProp(f, "BookingId", booking);

            var ci = DateTime.Now.AddDays(checkInOffsetDays);
            var co = DateTime.Now.AddDays(checkOutOffsetDays);
            SetDateFlex(f, "CheckInDate", ci);
            SetDateFlex(f, "CheckOutDate", co);

            if (FolioStatusEnum is not null)
            {
                var open = EnumTryParse(FolioStatusEnum, status);
                SetProp(f, "Status", open ?? GetProp(f, "Status"));
            }
            else
            {
                SetProp(f, "Status", status);
            }

            foreach (var (desc, amt, off) in initialCharges)
                AddToCharges(f, CreateLineItem(desc, amt, isPayment: false, method: null, when: DateTime.Now.AddDays(off)));

            foreach (var (desc, amt, off, method) in initialPayments)
                AddToPayments(f, CreateLineItem(desc, amt, isPayment: true, method: method, when: DateTime.Now.AddDays(off)));

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

                // DateOnly support
                if (targetType.FullName == "System.DateOnly" && value is DateTime dt)
                {
                    var fromDt = targetType.GetMethod("FromDateTime", BindingFlags.Public | BindingFlags.Static)!;
                    var dateOnly = fromDt.Invoke(null, new object[] { dt });
                    pi.SetValue(obj, dateOnly);
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

            if (v.GetType().FullName == "System.DateOnly")
            {
                var t = v.GetType();
                var y = (int)t.GetProperty("Year")!.GetValue(v)!;
                var m = (int)t.GetProperty("Month")!.GetValue(v)!;
                var d = (int)t.GetProperty("Day")!.GetValue(v)!;
                return new DateTime(y, m, d, 0, 0, 0);
            }

            if (DateTime.TryParse(v.ToString(), out var parsed)) return parsed;
            return null;
        }

        private static bool SetDateFlex(object obj, string name, DateTime when)
            => SetProp(obj, name, when) || SetProp(obj, name, when.Date);

        private static string FormatDate(DateTime? dt)
            => dt?.ToString("yyyy-MM-dd") ?? "—";

        private static object? EnumTryParse(Type enumType, string value)
        {
            try { return Enum.Parse(enumType, value, ignoreCase: true); }
            catch { return null; }
        }

        private static IList? GetIList(object obj, string name)
            => obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(obj) as IList;

        private IList? GetChargesIList(GuestFolio f) => GetIList(f, "Charges");
        private IList? GetPaymentsIList(GuestFolio f) => GetIList(f, "Payments");

        private List<object> GetChargesSidecar(GuestFolio f) => _sidecars.GetOrCreateValue(f).Charges;
        private List<object> GetPaymentsSidecar(GuestFolio f) => _sidecars.GetOrCreateValue(f).Payments;

        private IEnumerable<object> EnumerateCharges(GuestFolio f)
        {
            var il = GetChargesIList(f);
            if (il is not null) foreach (var o in il) yield return o!;
            else foreach (var o in GetChargesSidecar(f)) yield return o!;
        }

        private IEnumerable<object> EnumeratePayments(GuestFolio f)
        {
            var il = GetPaymentsIList(f);
            if (il is not null) foreach (var o in il) yield return o!;
            else foreach (var o in GetPaymentsSidecar(f)) yield return o!;
        }
    }
}
