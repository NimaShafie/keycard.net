using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeyCard.BusinessLogic.Commands.Guest.Invoice;
using KeyCard.BusinessLogic.ServiceInterfaces;
using KeyCard.BusinessLogic.ViewModels;
using KeyCard.Infrastructure.Models;
using KeyCard.Infrastructure.Models.AppDbContext;
using KeyCard.Infrastructure.Models.Bookings;
using KeyCard.Infrastructure.Models.Entities;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace KeyCard.Infrastructure.ServiceImplementation
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDBContext _context;
        private readonly IWebHostEnvironment _env;

        public InvoiceService(ApplicationDBContext context, IWebHostEnvironment env) {
            this._context = context;
            this._env = env;
        }

        public async Task<InvoiceViewModel> GenerateInvoiceAsync(GenerateInvoiceCommand command, CancellationToken ct)
        {
            var booking = await _context.Bookings
                .Include(booking => booking.Room)
                .Include(booking => booking.GuestProfile)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, ct);

            if (booking == null) {
                throw new InvalidOperationException($"Booking {command.BookingId} not found.");
            }

            //check if invoice already exists
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(x => x.BookingId == command.BookingId && !x.IsDeleted);

            if (existingInvoice != null)
            {
                return new InvoiceViewModel(existingInvoice.InvoiceNumber,
                                            existingInvoice.IssuedAt,
                                            existingInvoice.PdfPath,
                                            existingInvoice.TotalAmount,
                                            existingInvoice.BookingId);
            }

            var nights = Math.Max(1, (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days);
            var total = booking.TotalAmount;
            var fees = booking.ExtraFees;

            var taxes = Math.Round(total * 0.1m, 2); // assuming 10% tax

            var finalAmount = total + taxes + fees;

            var invoice = new Invoice
            {
                InvoiceNumber = await NextInvoiceNumberAsync(ct),
                IssuedAt = DateTime.UtcNow,
                TotalAmount = total,
                BookingId = booking.Id,
                PdfPath = "" // set after PDF is generated
            };

            await _context.Invoices.AddAsync(invoice, ct);
            await _context.SaveChangesAsync(ct);

            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => !h.IsDeleted, ct);

            // 5) Generate PDF with QuestPDF (replace layout as desired)
            var (relativePath, absolutePath) = EnsureInvoicePaths(invoice);
            GeneratePdf(absolutePath, booking, invoice, hotel, finalAmount, taxes, fees);

            // 6) Update PdfPath and save
            invoice.PdfPath = relativePath;
            await _context.SaveChangesAsync(ct);


            return new InvoiceViewModel(invoice.InvoiceNumber,
                                            invoice.IssuedAt,
                                            invoice.PdfPath,
                                            invoice.TotalAmount,
                                            invoice.BookingId);
        }

        public async Task<InvoiceViewModel> GetInvoicePdfAsync(GetInvoicePdfCommand command, CancellationToken ct)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.BookingId == command.BookingId && !i.IsDeleted);

            if (invoice is null || string.IsNullOrWhiteSpace(invoice.PdfPath))
                throw new KeyNotFoundException("Invoice Not Found");

            return new InvoiceViewModel(invoice.InvoiceNumber,
                                        invoice.IssuedAt,
                                        invoice.PdfPath,
                                        invoice.TotalAmount,
                                        invoice.BookingId);
        }
        private async Task<string> NextInvoiceNumberAsync(CancellationToken ct)
        {
            // Simple: INV-YYYYMMDD-#### (ensure uniqueness with DB index)
            var date = DateTime.UtcNow.ToString("yyyyMMdd");

            var todayCount = await _context.Invoices
                .CountAsync(i => i.IssuedAt.Date == DateTime.UtcNow.Date, ct);

            return $"INV-{date}-{(todayCount + 1).ToString().PadLeft(4, '0')}";
        }

        private (string relative, string absolute) EnsureInvoicePaths(Invoice invoice)
        {
            var wwwroot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");

            var dir = Path.Combine(wwwroot, "invoices");
            Directory.CreateDirectory(dir);

            var fileName = $"{invoice.InvoiceNumber}.pdf";
            var absolute = Path.Combine(dir, fileName);
            var relative = $"/invoices/{fileName}";

            return (relative, absolute);
        }

        private static void GeneratePdf(
              string absolutePath,
              Booking booking,
              Invoice invoice,
              Hotel hotel,
              decimal subtotal,
              decimal taxes,
              decimal fees)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(36);

                    page.Header().Column(h =>
                    {
                            h.Item().Text(hotel.Name).Bold().FontSize(26);
                            h.Item().Text(hotel.Address);
                            h.Item().Text(hotel.City + " " + hotel.Country);
                            var contactLine = string.Join("   ", new[]
                            {
                                $"Email: {hotel.ContactEmail}",
                                $"Phone: {hotel.ContactPhone}"
                            }.Where(x => x is not null)!);
                                    if (!string.IsNullOrWhiteSpace(contactLine)) h.Item().Text(contactLine);
                                    h.Item().PaddingVertical(6);
                                    h.Item().Text("Hotel Invoice").Bold().FontSize(18);
                                });

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Invoice #: {invoice.InvoiceNumber}");
                        col.Item().Text($"Issued: {invoice.IssuedAt:u}");
                        col.Item().PaddingVertical(10);

                        col.Item().Text($"Guest: {booking.GuestProfile.FullName}");
                        col.Item().Text($"Email: {booking.GuestProfile.Email}");
                        col.Item().Text($"Room: {booking.Room.RoomNumber}");
                        col.Item().Text($"Stay: {booking.CheckInDate:yyyy-MM-dd} → {booking.CheckOutDate:yyyy-MM-dd}");

                        col.Item().PaddingVertical(15);

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                            });

                            t.Header(h =>
                            {
                                h.Cell().Element(CellHeader).Text("Description");
                                h.Cell().Element(CellHeader).AlignRight().Text("Amount");
                            });

                            t.Cell().Element(Cell).Text($"Room ({booking.CheckInDate:MM/dd}–{booking.CheckOutDate:MM/dd})");
                            t.Cell().Element(Cell).AlignRight().Text($"{subtotal:C}");

                            if (fees > 0)
                            {
                                t.Cell().Element(Cell).Text("Fees / Extras");
                                t.Cell().Element(Cell).AlignRight().Text($"{fees:C}");
                            }

                            t.Cell().Element(Cell).Text("Taxes");
                            t.Cell().Element(Cell).AlignRight().Text($"{taxes:C}");

                            t.Cell().Element(CellHeader).Text("Total");
                            t.Cell().Element(CellHeader).AlignRight().Text($"{invoice.TotalAmount:C}");

                            static IContainer Cell(IContainer c) => c.PaddingVertical(4).BorderBottom(1);
                            static IContainer CellHeader(IContainer c) => c.PaddingVertical(6).BorderBottom(1);
                        });
                    });

                    page.Footer().AlignRight().Text(txt =>
                    {
                        txt.Span("Thank you for staying with us.");
                    });
                });
            }).GeneratePdf(absolutePath);
        }


    }
}
