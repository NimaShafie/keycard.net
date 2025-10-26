// /Modules/Folio/Utility/InvoicePaths.cs
using System;
using System.IO;

namespace KeyCard.Desktop.Modules.Folio.Utility
{
    internal static class InvoicePaths
    {
        public static string GetAppDataRoot()
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var app = Path.Combine(root, "KeyCard.NET", "Folio");
            return app;
        }

        public static string GetInvoicesFolder()
            => Path.Combine(GetAppDataRoot(), "Invoices");
    }
}
