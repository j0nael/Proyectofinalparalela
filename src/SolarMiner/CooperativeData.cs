using System.Globalization;

namespace SolarMiner;

/// <summary>
/// Genera y carga los datos de energía de la cooperativa solar.
/// Estas transacciones son el "mundo real" del proyecto (punto 6): los intercambios
/// de kWh entre las casas que la blockchain debe registrar de forma confiable.
/// </summary>
public static class CooperativeData
{
    private static readonly string[] Houses =
    {
        "Casa-01", "Casa-02", "Casa-03", "Casa-04", "Casa-05",
        "Casa-06", "Casa-07", "Casa-08", "Casa-09", "Casa-10"
    };

    /// <summary>
    /// Genera un lote de transacciones de energía simuladas pero realistas:
    /// unas casas aportan energía (paneles produciendo de más) y otras consumen.
    /// </summary>
    public static List<EnergyTransaction> GenerateTransactions(int count, int seed = 42)
    {
        var random = new Random(seed);
        var transactions = new List<EnergyTransaction>();
        var baseTime = new DateTime(2026, 8, 3, 8, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < count; i++)
        {
            string house = Houses[random.Next(Houses.Length)];
            // Más probabilidad de aportar en horas de sol (simulación simple).
            string type = random.NextDouble() < 0.6 ? "APORTA" : "CONSUME";
            double kWh = Math.Round(0.5 + random.NextDouble() * 6.0, 2);
            var timestamp = baseTime.AddMinutes(i * 3);

            transactions.Add(new EnergyTransaction(house, type, kWh, timestamp));
        }

        return transactions;
    }

    /// <summary>Guarda las transacciones en un CSV (para tenerlas como dato de entrada).</summary>
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

    /// <summary>Carga transacciones desde un CSV previamente generado.</summary>
    public static List<EnergyTransaction> LoadFromCsv(string path)
    {
        var transactions = new List<EnergyTransaction>();
        var lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++) // saltar cabecera
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
