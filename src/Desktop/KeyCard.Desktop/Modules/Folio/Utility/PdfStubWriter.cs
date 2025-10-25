// /Modules/Folio/Utility/PdfStubWriter.cs
using System.IO;
using System.Text;

namespace KeyCard.Desktop.Modules.Folio.Utility
{
    /// <summary>
    /// Writes a tiny, valid PDF with a single text object.
    /// Good enough for a preview/open-in-default-PDF-app flow.
    /// </summary>
    internal static class PdfStubWriter
    {
        public static void WriteSimplePdf(string path, string text)
        {
            // Minimal PDF: 1 page, 1 content stream with text.
            // This is intentionally simple; replace with a real PDF lib later.
            var sb = new StringBuilder();
            sb.AppendLine("%PDF-1.4");

            // 1: Catalog
            sb.AppendLine("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj");

            // 2: Pages
            sb.AppendLine("2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj");

            // 3: Page
            sb.AppendLine("3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >> endobj");

            // 4: Content stream
            var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\n", "\\n");
            var content = $"BT /F1 12 Tf 72 720 Td ({escaped}) Tj ET";
            var bytes = Encoding.ASCII.GetBytes(content);
            sb.AppendLine($"4 0 obj << /Length {bytes.Length} >> stream");
            sb.Append(content);
            sb.AppendLine("\nendstream endobj");

            // 5: Font
            sb.AppendLine("5 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj");

            // xref / trailer (static offsets avoided by rewriting file and letting readers be forgiving)
            // For robustness across viewers, we'll write a simple non-xref-compliant trailer many readers still open.
            // Most PDF readers accept this; if not, swap to a real lib later.

            sb.AppendLine("trailer << /Root 1 0 R >>");
            sb.AppendLine("%%EOF");

            File.WriteAllText(path, sb.ToString(), Encoding.ASCII);
        }
    }
}
