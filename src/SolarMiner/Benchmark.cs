using System.Globalization;
using System.Text;

namespace SolarMiner;

/// <summary>
/// Ejecuta el estudio de escalabilidad: mina el mismo bloque con distinta
/// cantidad de hilos y distintas dificultades, mide todo y lo vuelca a un CSV
/// listo para graficar en Excel. Cubre los puntos 4 (escalabilidad) y
/// 5 (métricas) del documento, y produce la evidencia de la sección 6
/// (comparativa secuencial vs paralela).
/// </summary>
public static class Benchmark
{
    public static void Run(Miner miner, List<EnergyTransaction> transactions)
    {
        Console.WriteLine();
        Console.WriteLine(new string('═', 70));
        Console.WriteLine("BENCHMARK DE ESCALABILIDAD");
        Console.WriteLine(new string('═', 70));

        int maxThreads = Environment.ProcessorCount;

        // Lista de conteos de hilos a probar: 1, 2, 4, 8... hasta el nº de núcleos.
        var threadCounts = new List<int>();
        for (int t = 1; t <= maxThreads; t *= 2)
            threadCounts.Add(t);
        if (!threadCounts.Contains(maxThreads))
            threadCounts.Add(maxThreads);

        // Dificultades a evaluar. La 6 ya tarda bastante; ajusta según tu máquina.
        int[] difficulties = { 4, 5, 6 };

        int repetitions = 3; // repetimos y promediamos para que la medición sea estable.

        // Timestamp e hash previo FIJOS: todas las corridas (secuencial y con N hilos,
        // en todas las dificultades) resuelven EXACTAMENTE el mismo acertijo. Sin esto,
        // cada bloque tendría una hora distinta y compararíamos problemas diferentes.
        var fixedTime = new DateTime(2026, 8, 3, 8, 0, 0, DateTimeKind.Utc);
        string prevHash = new string('0', 64);

        var csv = new StringBuilder();
        csv.AppendLine("Difficulty,Threads,AvgTimeSeconds,AvgHashRate,Speedup,Efficiency");

        foreach (int difficulty in difficulties)
        {
            Console.WriteLine($"\n>> Dificultad {difficulty} ceros");
            Console.WriteLine($"   {"Hilos",-6}{"Tiempo(s)",-12}{"MHash/s",-10}{"Speedup",-10}{"Eficiencia",-10}");

            double baselineTime = 0; // tiempo con 1 hilo, para calcular el speedup.

            foreach (int threads in threadCounts)
            {
                double totalTime = 0;
                double totalHashRate = 0;

                for (int rep = 0; rep < repetitions; rep++)
                {
                    // Bloque nuevo en cada corrida, pero con timestamp FIJO para que
                    // la búsqueda sea idéntica en todas las corridas y sea comparable.
                    var block = new Block(1, transactions, prevHash, fixedTime);

                    MiningResult result = threads == 1
                        ? miner.MineSequential(block, difficulty)
                        : miner.MineParallel(block, difficulty, threads);

                    totalTime += result.ElapsedSeconds;
                    totalHashRate += result.HashRate;
                }

                double avgTime = totalTime / repetitions;
                double avgHashRate = totalHashRate / repetitions;

                if (threads == 1) baselineTime = avgTime;
                double speedup = baselineTime / Math.Max(avgTime, 1e-9);
                double efficiency = speedup / threads * 100.0;

                Console.WriteLine($"   {threads,-6}{avgTime,-12:F3}{avgHashRate / 1e6,-10:F2}{speedup,-10:F2}{efficiency,-10:F1}");

                csv.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F4},{3:F0},{4:F3},{5:F1}",
                    difficulty, threads, avgTime, avgHashRate, speedup, efficiency));
            }
        }

        // Guardar el CSV en la carpeta /metrics del proyecto.
        string metricsDir = OutputPaths.GetResultsDir();
        string outPath = Path.Combine(metricsDir, "resultados_escalabilidad.csv");
        File.WriteAllText(outPath, csv.ToString());

        Console.WriteLine();
        Console.WriteLine($"Resultados guardados en: {outPath}");
        Console.WriteLine("Ábrelo en Excel para generar las gráficas de speedup y eficiencia.");
        Console.WriteLine();
    }
}
