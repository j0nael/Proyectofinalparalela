using System.Globalization;

namespace SolarMiner;

/// <summary>
/// Lee un CSV de reporte de energía (el que genera el proyecto) y lo convierte en
/// una lista de transacciones lista para sellar en un bloque.
///
/// Formato esperado del CSV:  Fecha,Vivienda,Tipo,kWh
/// Ejemplo de fila:           2026-08-01,Casa-01,APORTA,4.50
///
/// Esto representa la "entrada de datos" del sistema real: en una plataforma
/// completa, estas filas vendrían de los medidores de cada vivienda; aquí vienen
/// del CSV que produce el propio proyecto.
/// </summary>
public static class CsvEnergyReader
{
    /// <summary>Lee un CSV y devuelve sus transacciones de energía.</summary>
    public static List<EnergyTransaction> ReadFile(string path)
    {
        var transactions = new List<EnergyTransaction>();
        var lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++) // saltar la cabecera
        {
            string line = lines[i].Trim();
            if (line.Length == 0) continue;

            var parts = line.Split(',');
            if (parts.Length < 4) continue;

            string fecha = parts[0].Trim();
            string vivienda = parts[1].Trim();
            string tipo = parts[2].Trim();
            string kwhText = parts[3].Trim();

            // La fecha del reporte no lleva hora: la tomamos como medianoche UTC.
            DateTime timestamp = DateTime.Parse(
                fecha, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            double kWh = double.Parse(kwhText, CultureInfo.InvariantCulture);

            transactions.Add(new EnergyTransaction(vivienda, tipo, kWh, timestamp));
        }

        return transactions;
    }
}
