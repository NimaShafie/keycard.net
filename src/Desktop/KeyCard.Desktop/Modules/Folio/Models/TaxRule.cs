// /Modules/Folio/Models/TaxRule.cs
namespace KeyCard.Desktop.Modules.Folio.Models
{
    public sealed class TaxRule
    {
        public string Name { get; set; } = "Tax";
        public decimal Rate { get; set; } // e.g. 0.10m = 10%

        public decimal Compute(decimal baseAmount) => decimal.Round(baseAmount * Rate, 2);
    }
}
