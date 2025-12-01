// ============================================================================
// INVOICE SERVICE - PDF BILL GENERATION
// creates beautiful PDF invoices for guests at checkout
// includes room charges, taxes, fees - everything itemized
// uses QuestPDF library for PDF generation (pretty cool library btw!)
// ============================================================================

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
    /// <summary>
    /// Invoice service - generates PDF invoices for guests
    /// Called automatically at checkout, or guest can request anytime
    /// PDFs are stored in wwwroot/invoices folder for download
    /// </summary>
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDBContext _context;
        private readonly IWebHostEnvironment _env;  // for getting wwwroot path

        public InvoiceService(ApplicationDBContext context, IWebHostEnvironment env) {
            this._context = context;
            this._env = env;
        }

        /// <summary>
        /// Generate invoice PDF for a booking
        /// If invoice already exists, just return it (dont regenerate)
        /// </summary>
        public async Task<InvoiceViewModel> GenerateInvoiceAsync(GenerateInvoiceCommand command, CancellationToken ct)
        {
            // get booking with all related info we need for invoice
            var booking = await _context.Bookings
                .Include(booking => booking.Room)
                .Include(booking => booking.GuestProfile)
                .FirstOrDefaultAsync(b => b.Id == command.BookingId && !b.IsDeleted, ct);

            if (booking == null) {
                throw new InvalidOperationException($"Booking {command.BookingId} not found.");
            }

            // ===== Check if invoice already exists =====
            // dont want to create duplicate invoices!
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(x => x.BookingId == command.BookingId && !x.IsDeleted);

            if (existingInvoice != null)
            {
                // just return the existing one
                return new InvoiceViewModel(existingInvoice.InvoiceNumber,
                                            existingInvoice.IssuedAt,
                                            existingInvoice.PdfPath,
                                            existingInvoice.TotalAmount,
                                            existingInvoice.BookingId);
            }

            // ===== Calculate amounts =====
            var nights = Math.Max(1, (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days);
            var total = booking.TotalAmount;
            var fees = booking.ExtraFees;  // minibar, room service, etc.

            var taxes = Math.Round(total * 0.1m, 2); // 10% tax - TODO: make configurable

            var finalAmount = total + taxes + fees;

            // ===== Create invoice record =====
            var invoice = new Invoice
            {
                InvoiceNumber = await NextInvoiceNumberAsync(ct),  // e.g., INV-20240115-0001
                IssuedAt = DateTime.UtcNow,
                TotalAmount = total,
                BookingId = booking.Id,
                PdfPath = "" // set after PDF is generated
            };

            await _context.Invoices.AddAsync(invoice, ct);
            await _context.SaveChangesAsync(ct);

            // get hotel info for invoice header
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => !h.IsDeleted, ct);

            // ===== Generate the PDF =====
            var (relativePath, absolutePath) = EnsureInvoicePaths(invoice);
            GeneratePdf(absolutePath, booking, invoice, hotel, finalAmount, taxes, fees);

            // save the PDF path so we can find it later
            invoice.PdfPath = relativePath;
            await _context.SaveChangesAsync(ct);

            return new InvoiceViewModel(invoice.InvoiceNumber,
                                        invoice.IssuedAt,
                                        invoice.PdfPath,
                                        invoice.TotalAmount,
                                        invoice.BookingId);
        }

        /// <summary>
        /// Get existing invoice for a booking
        /// Used when guest wants to download their invoice again
        /// </summary>
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
        
        /// <summary>
        /// Generate unique invoice number for today
        /// Format: INV-YYYYMMDD-#### (e.g., INV-20240115-0042)
        /// </summary>
        private async Task<string> NextInvoiceNumberAsync(CancellationToken ct)
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");

            // count how many invoices already created today
            var todayCount = await _context.Invoices
                .CountAsync(i => i.IssuedAt.Date == DateTime.UtcNow.Date, ct);

            // pad with zeros to 4 digits: 1 → 0001, 42 → 0042
            return $"INV-{date}-{(todayCount + 1).ToString().PadLeft(4, '0')}";
        }

        /// <summary>
        /// Make sure the invoices folder exists and return file paths
        /// Creates folder if it doesnt exist
        /// </summary>
        private (string relative, string absolute) EnsureInvoicePaths(Invoice invoice)
        {
            var wwwroot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");

            // invoices go in wwwroot/invoices/
            var dir = Path.Combine(wwwroot, "invoices");
            Directory.CreateDirectory(dir);  // creates if not exists

            var fileName = $"{invoice.InvoiceNumber}.pdf";
            var absolute = Path.Combine(dir, fileName);
            var relative = $"/invoices/{fileName}";  // URL path for download

            return (relative, absolute);
        }

        /// <summary>
        /// Actually generate the PDF file using QuestPDF
        /// Creates a professional looking invoice with hotel header, line items, totals
        /// </summary>
        private static void GeneratePdf(
              string absolutePath,
              Booking booking,
              Invoice invoice,
              Hotel hotel,
              decimal subtotal,
              decimal taxes,
              decimal fees)
        {
            // QuestPDF community license is free for most uses
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(36);  // nice margins around the page

                    // ===== HEADER - Hotel info =====
                    page.Header().Column(h =>
                    {
                        h.Item().Text(hotel.Name).Bold().FontSize(26);  // big hotel name
                        h.Item().Text(hotel.Address);
                        h.Item().Text(hotel.City + " " + hotel.Country);
                        
                        // contact info on one line
                        var contactLine = string.Join("   ", new[]
                        {
                            $"Email: {hotel.ContactEmail}",
                            $"Phone: {hotel.ContactPhone}"
                        }.Where(x => x is not null)!);
                        
                        if (!string.IsNullOrWhiteSpace(contactLine)) h.Item().Text(contactLine);
                        h.Item().PaddingVertical(6);
                        h.Item().Text("Hotel Invoice").Bold().FontSize(18);
                    });

                    // ===== CONTENT - Invoice details and line items =====
                    page.Content().Column(col =>
                    {
                        // invoice metadata
                        col.Item().Text($"Invoice #: {invoice.InvoiceNumber}");
                        col.Item().Text($"Issued: {invoice.IssuedAt:u}");
                        col.Item().PaddingVertical(10);

                        // guest info
                        col.Item().Text($"Guest: {booking.GuestProfile.FullName}");
                        col.Item().Text($"Email: {booking.GuestProfile.Email}");
                        col.Item().Text($"Room: {booking.Room.RoomNumber}");
                        col.Item().Text($"Stay: {booking.CheckInDate:yyyy-MM-dd} → {booking.CheckOutDate:yyyy-MM-dd}");

                        col.Item().PaddingVertical(15);

                        // ===== Line items table =====
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);  // description takes 3/4 width
                                c.RelativeColumn(1);  // amount takes 1/4 width
                            });

                            // table header
                            t.Header(h =>
                            {
                                h.Cell().Element(CellHeader).Text("Description");
                                h.Cell().Element(CellHeader).AlignRight().Text("Amount");
                            });

                            // room charge
                            t.Cell().Element(Cell).Text($"Room ({booking.CheckInDate:MM/dd}–{booking.CheckOutDate:MM/dd})");
                            t.Cell().Element(Cell).AlignRight().Text($"{subtotal:C}");

                            // extra fees if any (minibar, room service, etc.)
                            if (fees > 0)
                            {
                                t.Cell().Element(Cell).Text("Fees / Extras");
                                t.Cell().Element(Cell).AlignRight().Text($"{fees:C}");
                            }

                            // taxes
                            t.Cell().Element(Cell).Text("Taxes");
                            t.Cell().Element(Cell).AlignRight().Text($"{taxes:C}");

                            // total - bold and prominent
                            t.Cell().Element(CellHeader).Text("Total");
                            t.Cell().Element(CellHeader).AlignRight().Text($"{invoice.TotalAmount:C}");

                            // helper functions for table cell styling
                            static IContainer Cell(IContainer c) => c.PaddingVertical(4).BorderBottom(1);
                            static IContainer CellHeader(IContainer c) => c.PaddingVertical(6).BorderBottom(1);
                        });
                    });

                    // ===== FOOTER - Thank you message =====
                    page.Footer().AlignRight().Text(txt =>
                    {
                        txt.Span("Thank you for staying with us.");
                    });
                });
            }).GeneratePdf(absolutePath);  // write to file!
        }
    }
}
