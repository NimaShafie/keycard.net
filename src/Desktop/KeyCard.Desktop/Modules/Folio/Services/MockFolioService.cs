// Modules/Folio/Services/MockFolioService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using KeyCard.Desktop.Modules.Folio.Models;

namespace KeyCard.Desktop.Modules.Folio.Services
{
    /// <summary>
    /// Mock implementation of IFolioService for development/testing.
    /// </summary>
    public class MockFolioService : IFolioService
    {
        private readonly List<GuestFolio> _folios = new();

        public MockFolioService()
        {
            // Create some mock data
            SeedMockData();
        }

        public Task<IReadOnlyList<GuestFolio>> GetActiveFoliosAsync()
        {
            var active = _folios.Where(f => f.Status == "Open").ToList();
            return Task.FromResult<IReadOnlyList<GuestFolio>>(active);
        }

        public Task<GuestFolio?> GetFolioByIdAsync(string folioId)
        {
            var folio = _folios.FirstOrDefault(f => f.FolioId == folioId);
            return Task.FromResult(folio);
        }

        public Task<IReadOnlyList<GuestFolio>> SearchFoliosAsync(string searchTerm)
        {
            var term = searchTerm.ToLowerInvariant();
            var results = _folios.Where(f =>
                f.GuestName.ToLowerInvariant().Contains(term) ||
                f.BookingId.ToLowerInvariant().Contains(term) ||
                f.RoomNumber.ToString().Contains(term)
            ).ToList();

            return Task.FromResult<IReadOnlyList<GuestFolio>>(results);
        }

        public Task PostChargeAsync(string folioId, decimal amount, string description)
        {
            var folio = _folios.FirstOrDefault(f => f.FolioId == folioId);
            if (folio != null)
            {
                var lineItem = new FolioLineItem
                {
                    Id = Guid.NewGuid().ToString(),
                    TransactionDate = DateTime.Now,
                    Type = "Charge",
                    Description = description,
                    Amount = amount
                };

                folio.LineItems.Add(lineItem);
                folio.TotalCharges += amount;
            }

            return Task.CompletedTask;
        }

        public Task ApplyPaymentAsync(string folioId, decimal amount, string paymentMethod)
        {
            var folio = _folios.FirstOrDefault(f => f.FolioId == folioId);
            if (folio != null)
            {
                var lineItem = new FolioLineItem
                {
                    Id = Guid.NewGuid().ToString(),
                    TransactionDate = DateTime.Now,
                    Type = "Payment",
                    Description = $"Payment - {paymentMethod}",
                    Amount = -amount // Negative for payment
                };

                folio.LineItems.Add(lineItem);
                folio.TotalPayments += amount;
            }

            return Task.CompletedTask;
        }

        public Task PrintStatementAsync(string folioId)
        {
            // Mock: just simulate delay
            return Task.Delay(500);
        }

        public Task CloseFolioAsync(string folioId)
        {
            var folio = _folios.FirstOrDefault(f => f.FolioId == folioId);
            if (folio != null)
            {
                folio.Status = "Closed";
            }

            return Task.CompletedTask;
        }

        private void SeedMockData()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            _folios.Add(new GuestFolio
            {
                FolioId = "F001",
                BookingId = "BK001",
                GuestName = "John Smith",
                RoomNumber = 101,
                CheckInDate = today.AddDays(-2),
                CheckOutDate = today.AddDays(1),
                TotalCharges = 450.00m,
                TotalPayments = 200.00m,
                Status = "Open",
                LineItems = new List<FolioLineItem>
                {
                    new FolioLineItem
                    {
                        Id = "L001",
                        TransactionDate = DateTime.Now.AddDays(-2),
                        Type = "Charge",
                        Description = "Room Charge - Night 1",
                        Amount = 150.00m
                    },
                    new FolioLineItem
                    {
                        Id = "L002",
                        TransactionDate = DateTime.Now.AddDays(-1),
                        Type = "Charge",
                        Description = "Room Charge - Night 2",
                        Amount = 150.00m
                    },
                    new FolioLineItem
                    {
                        Id = "L003",
                        TransactionDate = DateTime.Now.AddDays(-1),
                        Type = "Charge",
                        Description = "Mini Bar",
                        Amount = 25.00m
                    },
                    new FolioLineItem
                    {
                        Id = "L004",
                        TransactionDate = DateTime.Now.AddDays(-1),
                        Type = "Charge",
                        Description = "Room Service",
                        Amount = 75.00m
                    },
                    new FolioLineItem
                    {
                        Id = "L005",
                        TransactionDate = DateTime.Now,
                        Type = "Payment",
                        Description = "Payment - Credit Card",
                        Amount = -200.00m
                    }
                }
            });

            _folios.Add(new GuestFolio
            {
                FolioId = "F002",
                BookingId = "BK002",
                GuestName = "Jane Doe",
                RoomNumber = 205,
                CheckInDate = today.AddDays(-1),
                CheckOutDate = today.AddDays(2),
                TotalCharges = 300.00m,
                TotalPayments = 0.00m,
                Status = "Open",
                LineItems = new List<FolioLineItem>
                {
                    new FolioLineItem
                    {
                        Id = "L006",
                        TransactionDate = DateTime.Now.AddDays(-1),
                        Type = "Charge",
                        Description = "Room Charge - Night 1",
                        Amount = 180.00m
                    },
                    new FolioLineItem
                    {
                        Id = "L007",
                        TransactionDate = DateTime.Now,
                        Type = "Charge",
                        Description = "Parking Fee",
                        Amount = 20.00m
                    },
                    new FolioLineItem
                    {
                        Id = "L008",
                        TransactionDate = DateTime.Now,
                        Type = "Charge",
                        Description = "WiFi Upgrade",
                        Amount = 15.00m
                    }
                }
            });

            _folios.Add(new GuestFolio
            {
                FolioId = "F003",
                BookingId = "BK003",
                GuestName = "Bob Wilson",
                RoomNumber = 312,
                CheckInDate = today,
                CheckOutDate = today.AddDays(3),
                TotalCharges = 0.00m,
                TotalPayments = 0.00m,
                Status = "Open",
                LineItems = new List<FolioLineItem>()
            });
        }
    }
}
