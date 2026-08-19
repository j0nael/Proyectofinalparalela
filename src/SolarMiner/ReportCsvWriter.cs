using System.Globalization;
using System.Text;

namespace SolarMiner;

/// <summary>
/// Genera, desde C#, los CSV de reporte de energía por período (semanal, quincenal
/// o mensual) y los guarda en disco. Son los mismos reportes que produce la página
/// HTML, pero escritos por el programa para poder leerlos y sellarlos después.
/// Cada CSV cubre un trozo del mes con las 5 viviendas de la cooperativa.
/// </summary>
public static class ReportCsvWriter
{
    private static readonly string[] Houses =
        { "Casa-01", "Casa-02", "Casa-03", "Casa-04", "Casa-05" };

    private static readonly DateTime StartDate =
        new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    private record Chunk(string Name, string Label, int FromDay, int ToDay);

    private static List<Chunk> GetChunks(string period) => period.ToLowerInvariant() switch
    {
        "mensual" => new() { new("reporte_mensual", "Mes completo", 0, 29) },
        "semanal" => new()
        {
            new("reporte_semana_1", "Semana 1", 0, 6),
            new("reporte_semana_2", "Semana 2", 7, 13),
            new("reporte_semana_3", "Semana 3", 14, 20),
            new("reporte_semana_4", "Semana 4", 21, 27),
            new("reporte_semana_5", "Semana 5", 28, 29),
        },
        _ => new() // quincenal (por defecto)
        {
            new("reporte_quincena_1", "Quincena 1", 0, 14),
            new("reporte_quincena_2", "Quincena 2", 15, 29),
        },
    };

    /// <summary>
    /// Genera los CSV del período indicado en la carpeta dada.
    /// Devuelve las rutas de los archivos creados, en orden.
    /// </summary>
    public static List<string> GeneratePeriodCsvs(string period, string outDir)
    {
        Directory.CreateDirectory(outDir);
        var paths = new List<string>();

        foreach (var chunk in GetChunks(period))
        {
            var sb = new StringBuilder();
            sb.AppendLine("Fecha,Vivienda,Tipo,kWh");

            for (int day = chunk.FromDay; day <= chunk.ToDay; day++)
            {
                DateTime date = StartDate.AddDays(day);
                string fecha = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                for (int h = 0; h < Houses.Length; h++)
                {
                    // Valores deterministas (reproducibles) pero con aspecto realista.
                    double r1 = Pseudo(day * 100 + h);
                    double r2 = Pseudo(day * 100 + h + 50);
                    double produced = Math.Round(2.5 + r1 * 6.0, 2); // producción solar
                    double consumed = Math.Round(1.5 + r2 * 4.0, 2); // consumo

                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},APORTA,{2:F2}", fecha, Houses[h], produced));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},CONSUME,{2:F2}", fecha, Houses[h], consumed));
                }
            }

            string path = Path.Combine(outDir, chunk.Name + ".csv");
            File.WriteAllText(path, sb.ToString());
            paths.Add(path);
        }

        return paths;
    }

    /// <summary>
    /// Genera un número pseudoaleatorio reproducible en [0,1) a partir de una semilla.
    /// No necesita ser criptográfico: solo da variedad realista y estable a los datos.
    /// </summary>
    private static double Pseudo(int seed)
    {
        // Mezcla sencilla de bits (estilo xorshift) para dispersar la semilla.
        uint x = (uint)(seed * 2654435761u + 1013904223u);
        x ^= x << 13; x ^= x >> 17; x ^= x << 5;
        return (x % 100000) / 100000.0;
    }
}
