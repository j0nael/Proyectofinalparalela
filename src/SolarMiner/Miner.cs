using System.Buffers.Text;
using System.Diagnostics;
using System.Security.Cryptography;

namespace SolarMiner;

/// Resultado de un minado: incluye el bloque sellado y las métricas de rendimiento
/// que exige el documento (tiempo, hashes probados, hashes/segundo).

public class MiningResult
{
    public Block Block { get; set; } = null!;
    public long Nonce { get; set; }
    public string Hash { get; set; } = string.Empty;
    public double ElapsedSeconds { get; set; }
    public long HashesTried { get; set; }
    public int ThreadsUsed { get; set; }
    public int Difficulty { get; set; }

    public double HashRate => HashesTried / Math.Max(ElapsedSeconds, 1e-9);
}

/// El minero de la cooperativa solar. Contiene el corazón del proyecto:
/// la búsqueda del nonce, tanto en versión secuencial (1 trabajador) como
/// paralela (N trabajadores). La comparación entre ambas es la evidencia
/// central de "la ventaja del paralelismo".
public class Miner
{
    // Longitud máxima en dígitos de un long (para reservar el hueco del nonce).
    private const int MaxNonceDigits = 20;

    // ------------------------------------------------------------------
    //  Comprobación de dificultad
    // ------------------------------------------------------------------

    /// Versión sobre TEXTO hexadecimal: ¿el hash empieza con 'difficulty' ceros?
    /// Se conserva para las pruebas y para validar resultados ya calculados.
    public static bool MeetsDifficulty(string hash, int difficulty)
    {
        for (int i = 0; i < difficulty; i++)
            if (hash[i] != '0') return false;
        return true;
    }

    /// Versión RÁPIDA sobre los BYTES del hash (la que se usa en el bucle caliente).
    ///
    /// Cada carácter '0' del hash hexadecimal equivale a un "nibble" (4 bits) en cero.
    /// Dos ceros hex = un byte completo en cero. Así, exigir 'difficulty' ceros hex es:
    ///   - que los primeros (difficulty / 2) bytes sean 0, y
    ///   - si 'difficulty' es impar, que el nibble ALTO del byte siguiente sea 0.
    ///
    /// Esto evita convertir el hash a texto en cada intento (millones de veces),
    /// que era uno de los mayores desperdicios del código original.

    private static bool MeetsDifficultyBytes(ReadOnlySpan<byte> hash, int difficulty)
    {
        int fullBytes = difficulty >> 1;         
        for (int i = 0; i < fullBytes; i++)
            if (hash[i] != 0) return false;

        if ((difficulty & 1) == 1)                   
            if ((hash[fullBytes] & 0xF0) != 0) return false;

        return true;
    }

    // ------------------------------------------------------------------
    //  VERSIÓN SECUENCIAL  (1 trabajador)Solo  — la línea base de comparación
    // ------------------------------------------------------------------

    /// Sella el bloque probando nonces uno tras otro con un solo hilo.
    /// Esta es la "versión de 1 trabajador" contra la que comparamos todo.
    public MiningResult MineSequential(Block block, int difficulty)
    {
        var stopwatch = Stopwatch.StartNew();


        byte[] prefix = block.GetContentPrefixBytes();
        byte[] buffer = new byte[prefix.Length + MaxNonceDigits];
        Array.Copy(prefix, buffer, prefix.Length);
        Span<byte> hash = stackalloc byte[32]; 

        long hashesTried = 0;
        long nonce = 0;

        while (true)
        {
            Utf8Formatter.TryFormat(nonce, buffer.AsSpan(prefix.Length), out int written);
            int totalLen = prefix.Length + written;

            SHA256.HashData(buffer.AsSpan(0, totalLen), hash);
            hashesTried++;

            if (MeetsDifficultyBytes(hash, difficulty))
            {
                stopwatch.Stop();
                block.Nonce = nonce;
                block.Hash = block.ComputeHash();

                return new MiningResult
                {
                    Block = block,
                    Nonce = nonce,
                    Hash = block.Hash,
                    ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
                    HashesTried = hashesTried,
                    ThreadsUsed = 1,
                    Difficulty = difficulty
                };
            }
            nonce++;
        }
    }

    // ------------------------------------------------------------------
    //  VERSIÓN PARALELA  (N trabajadores) — el núcleo del proyecto
    // ------------------------------------------------------------------

    /// Sella el bloque repartiendo la búsqueda de nonces entre 'threadCount' hilos.
    ///
    /// Estrategia: PARTICIÓN POR INTERCALADO (striding).
    ///   - El hilo 0 prueba los nonces 0, N, 2N, 3N...
    ///   - El hilo 1 prueba los nonces 1, N+1, 2N+1...
    ///   - ...y así. Entre todos cubren TODOS los nonces sin repetir ninguno.
    ///
    /// Sincronización (el punto 2 de la consigna):
    ///   - CancellationTokenSource = el "cartel de ¡ya lo encontré!". Cuando un hilo
    ///     acierta, lo levanta (Cancel) y los demás lo ven y paran.
    ///   - Interlocked = para sumar el total de hashes y fijar el nonce ganador sin
    ///     que dos hilos se pisen (evita condiciones de carrera).
    public MiningResult MineParallel(Block block, int difficulty, int threadCount)
    {
        var stopwatch = Stopwatch.StartNew();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        byte[] prefix = block.GetContentPrefixBytes();

        long totalHashesTried = 0;      
        long winningNonce = -1;       

        var tasks = new Task[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            int offset = t;
            tasks[t] = Task.Run(() =>
            {
                byte[] buffer = new byte[prefix.Length + MaxNonceDigits];
                Array.Copy(prefix, buffer, prefix.Length);
                Span<byte> hash = stackalloc byte[32];

                long localHashes = 0;
                long nonce = offset;

                while (!token.IsCancellationRequested)
                {
                    Utf8Formatter.TryFormat(nonce, buffer.AsSpan(prefix.Length), out int written);
                    SHA256.HashData(buffer.AsSpan(0, prefix.Length + written), hash);
                    localHashes++;

                    if (MeetsDifficultyBytes(hash, difficulty))
                    {

                        Interlocked.CompareExchange(ref winningNonce, nonce, -1);
                        cts.Cancel();
                        break;
                    }

                    nonce += threadCount;

                    if ((localHashes & 0x3FFF) == 0)
                    {
                        Interlocked.Add(ref totalHashesTried, localHashes);
                        localHashes = 0;
                    }
                }

                Interlocked.Add(ref totalHashesTried, localHashes);
            });
        }

        Task.WaitAll(tasks);
        stopwatch.Stop();

        block.Nonce = winningNonce;
        block.Hash = block.ComputeHash();

        return new MiningResult
        {
            Block = block,
            Nonce = winningNonce,
            Hash = block.Hash,
            ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
            HashesTried = totalHashesTried,
            ThreadsUsed = threadCount,
            Difficulty = difficulty
        };
    }
}
