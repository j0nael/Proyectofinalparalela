using System.Globalization;

namespace SolarMiner;
public static class CooperativeData
{
    private static readonly string[] Houses =
    {
        "Casa-01", "Casa-02", "Casa-03", "Casa-04", "Casa-05",
        "Casa-06", "Casa-07", "Casa-08", "Casa-09", "Casa-10"
    };

   
    public static List<EnergyTransaction> GenerateTransactions(int count, int seed = 42)
    {
        var random = new Random(seed);
        var transactions = new List<EnergyTransaction>();
        var baseTime = new DateTime(2026, 8, 3, 8, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < count; i++)
        {
            string house = Houses[random.Next(Houses.Length)];
           
            string type = random.NextDouble() < 0.6 ? "APORTA" : "CONSUME";
            double kWh = Math.Round(0.5 + random.NextDouble() * 6.0, 2);
            var timestamp = baseTime.AddMinutes(i * 3);

            transactions.Add(new EnergyTransaction(house, type, kWh, timestamp));
        }

        return transactions;
    }

  
    public static void SaveToCsv(List<EnergyTransaction> transactions, string path)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("House,Type,KWh,Timestamp");
        foreach (var tx in transactions)
        {
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2:F2},{3:yyyy-MM-ddTHH:mm:ss}",
                tx.House, tx.Type, tx.KWh, tx.Timestamp));
        }
    }

    
    public static List<EnergyTransaction> LoadFromCsv(string path)
    {
        var transactions = new List<EnergyTransaction>();
        var lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++) 
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 4) continue;

            transactions.Add(new EnergyTransaction(
                parts[0],
                parts[1],
                double.Parse(parts[2], CultureInfo.InvariantCulture),
                DateTime.Parse(parts[3], CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
            ));
        }

        return transactions;
    }
}
