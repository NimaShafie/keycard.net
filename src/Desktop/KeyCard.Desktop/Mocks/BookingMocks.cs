// Mocks/BookingMocks.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Mocks
{
    public static class BookingMocks
    {
        public static List<Booking> GetArrivalsToday(int count = 8) => Generate(count, false);
        public static List<Booking> GetDeparturesToday(int count = 4) => Generate(count, true);

        private static List<Booking> Generate(int count, bool isDeparture)
        {
            var list = new List<Booking>(count);
            var today = DateTime.Now.Date;

            for (int i = 1; i <= count; i++)
            {
                var id = Guid.NewGuid();
                var guest = isDeparture ? $"Departing {i}" : $"Guest {i}";
                var room = 200 + i;
                var checkIn = isDeparture ? today.AddDays(-3) : today;
                var checkOut = isDeparture ? today : today.AddDays(2);

                list.Add(CreateFlexible(id, guest, room, checkIn, checkOut));
            }
            return list;
        }

        // ---- Flexible creation for immutable/record types ----
        private static Booking CreateFlexible(Guid id, string guest, int room, DateTime checkIn, DateTime checkOut)
        {
            var t = typeof(Booking);

            // 1) Try ctors (any ordering of supported primitives)
            foreach (var ctor in t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                  .OrderByDescending(c => c.GetParameters().Length))
            {
                var ps = ctor.GetParameters();
                if (ps.Length == 0) continue;

                if (TryMapArgs(ps, id, guest, room, checkIn, checkOut, out var args))
                {
                    try { return (Booking)ctor.Invoke(args); } catch { /* try next ctor */ }
                }
            }

            // 2) Try parameterless + settable props
            var defaultCtor = t.GetConstructor(Type.EmptyTypes)
                           ?? t.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, binder: null, types: Type.EmptyTypes, modifiers: null);
            if (defaultCtor is not null)
            {
                try
                {
                    var obj = defaultCtor.Invoke(null);
                    if (TryAssignMembers(obj, id, guest, room, checkIn, checkOut, preferProperties: true))
                        return (Booking)obj;
                }
                catch { /* fall through */ }
            }

            // No obsolete last-resort path; fail clearly so you can align the model/mocks.
            throw new InvalidOperationException(
                $"Mock could not construct a {t.FullName}. Please expose either " +
                $"a constructor or settable properties for: Id/BookingId(Guid), GuestName(string), " +
                $"RoomNumber(int), CheckIn/CheckOut(DateOnly/DateTime).");
        }

        private static bool TryMapArgs(ParameterInfo[] ps, Guid id, string guest, int room, DateTime checkIn, DateTime checkOut, out object?[] args)
        {
            args = new object?[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                var name = p.Name?.ToLowerInvariant() ?? string.Empty;
                var type = Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType;

                if (type == typeof(Guid)) { args[i] = id; continue; }
                if (type == typeof(string)) { args[i] = name.Contains("guest") || name.Contains("name") ? guest : room.ToString(CultureInfo.InvariantCulture); continue; }
                if (type == typeof(int)) { args[i] = room; continue; }
                if (type == typeof(DateOnly)) { args[i] = DateOnly.FromDateTime(name.Contains("out") ? checkOut : checkIn); continue; }
                if (type == typeof(DateTime)) { args[i] = name.Contains("out") ? checkOut : checkIn; continue; }

                if (p.HasDefaultValue) { args[i] = p.DefaultValue; continue; }
                return false; // unsupported parameter
            }
            return true;
        }

        private static bool TryAssignMembers(object obj, Guid id, string guest, int room, DateTime checkIn, DateTime checkOut, bool preferProperties)
        {
            var t = obj.GetType();
            bool ok = false;

            bool SetProperty(string logical, object value)
            {
                var pi = FindProperty(t, logical);
                if (pi is { CanWrite: true })
                {
                    try
                    {
                        if (pi.PropertyType == typeof(DateOnly) && value is DateTime dt)
                            value = DateOnly.FromDateTime(dt);
                        pi.SetValue(obj, value);
                        return true;
                    }
                    catch { }
                }
                return false;
            }

            bool SetField(string logical, object value)
            {
                foreach (var name in new[]
                {
                    logical,
                    $"<{logical}>k__BackingField", // C# auto-prop
                    logical + "BackingField",
                    "_" + logical
                })
                {
                    var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fi is null) continue;
                    try
                    {
                        if (fi.FieldType == typeof(DateOnly) && value is DateTime dt)
                            value = DateOnly.FromDateTime(dt);
                        fi.SetValue(obj, value);
                        return true;
                    }
                    catch { }
                }
                return false;
            }

            // Try properties first (if requested), then fields, then properties again
            bool Set(string logical, object value)
            {
                bool assigned = false;
                if (preferProperties) assigned |= SetProperty(logical, value);
                assigned |= SetField(logical, value);
                if (!preferProperties) assigned |= SetProperty(logical, value);
                ok |= assigned;
                return assigned;
            }

            // Map common logical names (separate statements; no '||' chains)
            Set("BookingId", id);
            Set("Id", id);

            Set("GuestName", guest);
            Set("Name", guest);

            Set("RoomNumber", room);
            Set("Room", room);

            Set("CheckIn", checkIn);
            Set("CheckOut", checkOut);

            return ok;
        }

        private static PropertyInfo? FindProperty(Type t, string logicalName)
        {
            foreach (var n in new[] { logicalName, ToPascal(logicalName), ToCamel(logicalName) })
            {
                var pi = t.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (pi is not null) return pi;
            }
            return null;
        }

        private static string ToPascal(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
        private static string ToCamel(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
    }
}
