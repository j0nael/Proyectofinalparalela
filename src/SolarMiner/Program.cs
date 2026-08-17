using SolarMiner;

// ============================================================================
//  NODO MINERO PARA UNA COOPERATIVA DE ENERGÍA SOLAR
//  Proyecto de Programación Paralela — Descomposición Exploratoria
//
//  Este programa demuestra la ventaja del paralelismo sellando (minando)
//  bloques de una blockchain que registra intercambios de energía solar.
//  Compara la ejecución SECUENCIAL (1 hilo) contra la PARALELA (N hilos).
// ============================================================================

Console.OutputEncoding = System.Text.Encoding.UTF8;

PrintHeader();

// --- Paso 1: preparar los datos de la cooperativa (el "mundo real") ---
Console.WriteLine("Generando transacciones de energía de la cooperativa...");
var transactions = CooperativeData.GenerateTransactions(count: 20);

// Guardamos el CSV como dato de entrada verificable.
string dataDir = OutputPaths.GetResultsDir();
string csvPath = Path.Combine(dataDir, "energia_cooperativa.csv");
CooperativeData.SaveToCsv(transactions, csvPath);
Console.WriteLine($"  {transactions.Count} transacciones guardadas en: {csvPath}\n");

// Mostramos algunas para que se vea el "mundo real".
Console.WriteLine("Primeras transacciones registradas:");
foreach (var tx in transactions.Take(5))
    Console.WriteLine($"    {tx}");
Console.WriteLine("    ...\n");

var miner = new Miner();
int difficulty = 5; // nº de ceros exigidos. Súbelo para hacerlo más difícil.

// Timestamp FIJO: así el bloque plantea SIEMPRE el mismo acertijo, y la comparación
// entre secuencial y paralelo es justa (ambos resuelven exactamente el mismo problema).
var fixedTime = new DateTime(2026, 8, 3, 8, 0, 0, DateTimeKind.Utc);
string prevHash = new string('0', 64);

// --- Paso 2: minado SECUENCIAL (línea base) ---
Console.WriteLine(new string('─', 70));
Console.WriteLine($"MINADO SECUENCIAL (1 hilo) — dificultad {difficulty} ceros");
Console.WriteLine(new string('─', 70));

var blockSeq = new Block(1, transactions, prevHash, fixedTime);
var resultSeq = miner.MineSequential(blockSeq, difficulty);
PrintResult(resultSeq);

// --- Paso 3: minado PARALELO con todos los núcleos disponibles ---
int cores = Environment.ProcessorCount;
Console.WriteLine(new string('─', 70));
Console.WriteLine($"MINADO PARALELO ({cores} hilos) — dificultad {difficulty} ceros");
Console.WriteLine(new string('─', 70));

var blockPar = new Block(1, transactions, prevHash, fixedTime);
var resultPar = miner.MineParallel(blockPar, difficulty, cores);
PrintResult(resultPar);

// --- Paso 4: la comparación que pide el documento ---
Console.WriteLine(new string('═', 70));
Console.WriteLine("COMPARATIVA — la ventaja del paralelismo");
Console.WriteLine(new string('═', 70));
double speedup = resultSeq.ElapsedSeconds / Math.Max(resultPar.ElapsedSeconds, 1e-9);
double efficiency = speedup / cores * 100.0;
Console.WriteLine($"  Tiempo secuencial : {resultSeq.ElapsedSeconds,8:F3} s");
Console.WriteLine($"  Tiempo paralelo   : {resultPar.ElapsedSeconds,8:F3} s  ({cores} hilos)");
Console.WriteLine($"  Aceleración       : {speedup,8:F2}x  (cuántas veces más rápido)");
Console.WriteLine($"  Eficiencia        : {efficiency,8:F1}%  (aprovechamiento por hilo)");
Console.WriteLine();
Console.WriteLine("Para el estudio completo de escalabilidad ejecuta:  dotnet run -- benchmark");
Console.WriteLine();

// --- Modo benchmark opcional: barre hilos y dificultades y saca un CSV ---
if (args.Length > 0 && args[0].Equals("benchmark", StringComparison.OrdinalIgnoreCase))
{
    Benchmark.Run(miner, transactions);
}

// --- Modo cadena: construye una blockchain completa, la verifica y la exporta ---
if (args.Length > 0 && args[0].Equals("chain", StringComparison.OrdinalIgnoreCase))
{
    BuildAndExportChain(cores);
}

// --- Modo reporte: genera la página de reportes CSV y la abre en el navegador ---
if (args.Length > 0 && args[0].Equals("reporte", StringComparison.OrdinalIgnoreCase))
{
    GenerateAndOpenReport();
}

// --- Modo sellar: lee los CSV de reportes y sella un bloque por cada período ---
if (args.Length > 0 && args[0].Equals("sellar", StringComparison.OrdinalIgnoreCase))
{
    // Período opcional: dotnet run -- sellar quincenal   (o semanal / mensual)
    string period = args.Length > 1 ? args[1].ToLowerInvariant() : "quincenal";
    SealFromCsv(period, cores);
}


// ---------------------------------------------------------------------------
//  Fase completa: CSV de energía  ->  bloques sellados  ->  cadena verificable
// ---------------------------------------------------------------------------
static void SealFromCsv(string period, int threads)
{
    Console.WriteLine(new string('═', 70));
    Console.WriteLine("SELLADO DE LA CADENA A PARTIR DE LOS CSV DE ENERGÍA");
    Console.WriteLine(new string('═', 70));

    string resultsDir = OutputPaths.GetResultsDir();
    string reportsDir = Path.Combine(resultsDir, "reportes");
    Directory.CreateDirectory(reportsDir);

    // 1) Buscar CSV ya existentes en la carpeta de reportes. Si no hay, generarlos.
    var csvFiles = Directory.GetFiles(reportsDir, "*.csv")
                            .OrderBy(f => f, StringComparer.Ordinal)
                            .ToList();

    if (csvFiles.Count == 0)
    {
        Console.WriteLine($"  No hay CSV en la carpeta. Generando reportes «{period}»...");
        csvFiles = ReportCsvWriter.GeneratePeriodCsvs(period, reportsDir);
    }
    else
    {
        Console.WriteLine($"  Usando {csvFiles.Count} CSV encontrados en: {reportsDir}");
    }
    Console.WriteLine();

    // 2) Leer cada CSV y sellar un BLOQUE por cada uno, encadenándolos.
    int difficulty = 5;
    var chain = new Blockchain(difficulty);
    var baseTime = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    for (int i = 0; i < csvFiles.Count; i++)
    {
        var txs = CsvEnergyReader.ReadFile(csvFiles[i]);
        var res = chain.AddBlock(txs, threads, baseTime.AddHours(i));
        string fileName = Path.GetFileName(csvFiles[i]);
        Console.WriteLine($"  Bloque #{i} sellado desde «{fileName}»");
        Console.WriteLine($"      {txs.Count} transacciones · nonce {res.Nonce:N0} · hash {res.Hash[..16]}...");
    }

    // 3) Verificar la integridad de la cadena resultante.
    Console.WriteLine();
    Console.WriteLine(chain.IsValid()
        ? "  ✔ Cadena sellada y verificada: está INTACTA."
        : "  ✘ La cadena presenta alteraciones.");

    // 4) Exportar el JSON sellado y el visor HTML interactivo.
    string jsonPath = Path.Combine(resultsDir, "blockchain.json");
    string htmlPath = Path.Combine(resultsDir, "blockchain_visor.html");
    chain.ExportJson(jsonPath);
    chain.ExportHtml(htmlPath);

    Console.WriteLine();
    Console.WriteLine($"  Archivo sellado (JSON) : {jsonPath}");
    Console.WriteLine($"  Visor interactivo (HTML): {htmlPath}");
    Console.WriteLine("  Abre el visor y edita un dato para ver cómo se rompe la cadena.");

    // Intentar abrir el visor automáticamente.
    try
    {
        var psi = new System.Diagnostics.ProcessStartInfo(htmlPath) { UseShellExecute = true };
        System.Diagnostics.Process.Start(psi);
    }
    catch { /* si no se puede abrir solo, el usuario lo abre con doble clic */ }
    Console.WriteLine();
}


// ---------------------------------------------------------------------------
//  Genera la página de reportes de energía y la abre en el navegador
// ---------------------------------------------------------------------------
static void GenerateAndOpenReport()
{
    Console.WriteLine(new string('═', 70));
    Console.WriteLine("GENERADOR DE REPORTES DE ENERGÍA (CSV por período)");
    Console.WriteLine(new string('═', 70));

    string dir = OutputPaths.GetResultsDir();
    string htmlPath = Path.Combine(dir, "reporte_energia.html");
    File.WriteAllText(htmlPath, ReportGenerator.BuildHtml());

    Console.WriteLine($"  Página de reportes generada en: {htmlPath}");

    // Intentamos abrirla automáticamente en el navegador predeterminado.
    try
    {
        var psi = new System.Diagnostics.ProcessStartInfo(htmlPath) { UseShellExecute = true };
        System.Diagnostics.Process.Start(psi);
        Console.WriteLine("  Abriendo en el navegador...");
    }
    catch
    {
        Console.WriteLine("  (No se pudo abrir automáticamente; ábrela con doble clic.)");
    }
    Console.WriteLine();
}


// ---------------------------------------------------------------------------
//  Construcción de la cadena completa + verificación + exportación
// ---------------------------------------------------------------------------
static void BuildAndExportChain(int threads)
{
    Console.WriteLine(new string('═', 70));
    Console.WriteLine("CONSTRUCCIÓN DE LA CADENA DE BLOQUES");
    Console.WriteLine(new string('═', 70));

    int difficulty = 5;
    var chain = new Blockchain(difficulty);
    var baseTime = new DateTime(2026, 8, 3, 8, 0, 0, DateTimeKind.Utc);

    // Creamos 4 bloques, cada uno con un grupo de transacciones de energía.
    // Cada bloque se encadena y sella tomando el hash del anterior.
    for (int i = 0; i < 4; i++)
    {
        var txs = CooperativeData.GenerateTransactions(count: 4, seed: 100 + i);
        // Timestamp fijo por bloque (determinista y reproducible).
        var blockTime = baseTime.AddHours(i);
        var res = chain.AddBlock(txs, threads, blockTime);
        Console.WriteLine($"  Bloque #{i} sellado — nonce {res.Nonce:N0}, hash {res.Hash[..16]}...");
    }

    // Verificamos que la cadena esté intacta.
    Console.WriteLine();
    Console.WriteLine(chain.IsValid()
        ? "  ✔ Verificación: la cadena está INTACTA."
        : "  ✘ Verificación: la cadena está ALTERADA.");

    // Demostración de alteración desde el propio C# (además del visor HTML):
    Console.WriteLine();
    Console.WriteLine("  Simulando una alteración en el bloque #1...");
    chain.Blocks[1].Transactions[0].KWh = 999.99; // alguien cambia un dato
    int tampered = chain.FindFirstTamperedBlock();
    Console.WriteLine(tampered == -1
        ? "  (no se detectó alteración)"
        : $"  ✘ Alteración detectada: la cadena se rompe desde el bloque #{tampered}.");
    chain.Blocks[1].Transactions[0].KWh = 0; // (el dato ya no coincide; solo era demo)

    // Reconstruimos la cadena limpia para exportar los archivos correctos.
    var cleanChain = new Blockchain(difficulty);
    for (int i = 0; i < 4; i++)
    {
        var txs = CooperativeData.GenerateTransactions(count: 4, seed: 100 + i);
        cleanChain.AddBlock(txs, threads, baseTime.AddHours(i));
    }

    // Exportamos el JSON (archivo sellado) y el visor HTML interactivo.
    string outDir = OutputPaths.GetResultsDir();
    string jsonPath = Path.Combine(outDir, "blockchain.json");
    string htmlPath = Path.Combine(outDir, "blockchain_visor.html");
    cleanChain.ExportJson(jsonPath);
    cleanChain.ExportHtml(htmlPath);

    Console.WriteLine();
    Console.WriteLine($"  Archivo sellado (JSON) : {jsonPath}");
    Console.WriteLine($"  Visor interactivo (HTML): {htmlPath}");
    Console.WriteLine("  Abre el HTML con doble clic y edita un dato para ver la cadena romperse.");
    Console.WriteLine();
}


// ---------------------------------------------------------------------------
//  Funciones de presentación
// ---------------------------------------------------------------------------
static void PrintHeader()
{
    Console.WriteLine();
    Console.WriteLine("╔════════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║   NODO MINERO — COOPERATIVA DE ENERGÍA SOLAR (Blockchain)           ║");
    Console.WriteLine("║   Demostración de paralelismo por descomposición exploratoria       ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
}

static void PrintResult(MiningResult r)
{
    Console.WriteLine($"  Nonce encontrado  : {r.Nonce:N0}");
    Console.WriteLine($"  Hash              : {r.Hash}");
    Console.WriteLine($"  Hashes probados   : {r.HashesTried:N0}");
    Console.WriteLine($"  Tiempo            : {r.ElapsedSeconds:F3} s");
    Console.WriteLine($"  Velocidad         : {r.HashRate / 1000.0:N0} mil hashes/s");
    Console.WriteLine();
}
