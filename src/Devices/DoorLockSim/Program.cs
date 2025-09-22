using System.Text;

Console.WriteLine("KeyCard.NET Door Lock Simulator");
Console.Write("Paste digital key (placeholder string is fine) and press Enter: ");
var token = Console.ReadLine();

if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("No key provided.");
    return;
}

// TODO: validate JWT/HMAC later. For now, accept any non-empty string.
Console.WriteLine($"Key accepted at {DateTime.UtcNow:o}. Simulating door unlock...");
await Task.Delay(750);
Console.WriteLine("Door unlocked.");
