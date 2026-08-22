using System.Globalization;
using System.Text;

namespace SolarMiner;

/// La cadena de bloques completa de la cooperativa. Es lo que hace que esto sea
/// una "blockchain" y no solo un bloque suelto: cada bloque guarda el hash del
/// anterior, de modo que alterar un bloque viejo rompe el enlace con el siguiente
/// y toda la cadena a partir de ahí queda inválida (el "efecto dominó").
public class Blockchain
{
    ///Los bloques, en orden. El primero (índice 0) es el bloque génesis.
    public List<Block> Blocks { get; } = new();

    ///Dificultad usada para sellar (nº de ceros exigidos). Se guarda para el visor.
    public int Difficulty { get; }

    private readonly Miner _miner = new();

    // Hash del "bloque cero" imaginario: 64 ceros. Es el punto de partida de la cadena.
    public const string GenesisPreviousHash =
        "0000000000000000000000000000000000000000000000000000000000000000";

    public Blockchain(int difficulty)
    {
        Difficulty = difficulty;
    }

    /// Crea un bloque con las transacciones dadas, lo encadena al último bloque
    /// (tomando su hash como PreviousHash), lo SELLA en paralelo y lo añade.
    public MiningResult AddBlock(List<EnergyTransaction> transactions, int threadCount,
                                 DateTime? timestamp = null)
    {
        int index = Blocks.Count;
        string previousHash = Blocks.Count == 0 ? GenesisPreviousHash : Blocks[^1].Hash;

        var block = new Block(index, transactions, previousHash, timestamp);
        var result = _miner.MineParallel(block, Difficulty, threadCount);
        Blocks.Add(block);
        return result;
    }

    /// Verifica que la cadena esté intacta. Devuelve el índice del primer bloque
    /// alterado, o -1 si toda la cadena es válida. Comprueba dos cosas por bloque:
    ///   1) Que su hash guardado siga correspondiendo a su contenido (no se alteró el dato).
    ///   2) Que su PreviousHash coincida con el hash real del bloque anterior (enlace intacto).
    public int FindFirstTamperedBlock()
    {
        for (int i = 0; i < Blocks.Count; i++)
        {
            var b = Blocks[i];

            // (1) ¿El hash guardado sigue correspondiendo al contenido actual?
            string recomputed = Block.ComputeHash(b.GetBlockContent(b.Nonce));
            if (recomputed != b.Hash)
                return i;

            // (2) ¿El hash cumple todavía la dificultad?
            if (!Miner.MeetsDifficulty(b.Hash, Difficulty))
                return i;

            // (3) ¿El enlace con el bloque anterior es correcto?
            string expectedPrev = i == 0 ? GenesisPreviousHash : Blocks[i - 1].Hash;
            if (b.PreviousHash != expectedPrev)
                return i;
        }
        return -1; // cadena intacta
    }

    ///Atajo: ¿está toda la cadena intacta?
    public bool IsValid() => FindFirstTamperedBlock() == -1;

    // ------------------------------------------------------------------
    //  Exportación a archivos
    // ------------------------------------------------------------------

    /// Exporta la cadena a un archivo JSON legible. Este es el "archivo sellado"
    /// que se puede guardar, compartir y volver a abrir. Cada bloque se guarda con
    /// sus transacciones ya formateadas exactamente como se hashearon, para que
    /// cualquiera pueda re-verificar el sello.
    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"difficulty\": {Difficulty},");
        sb.AppendLine($"  \"genesisPreviousHash\": \"{GenesisPreviousHash}\",");
        sb.AppendLine("  \"blocks\": [");

        for (int i = 0; i < Blocks.Count; i++)
        {
            var b = Blocks[i];
            sb.AppendLine("    {");
            sb.AppendLine($"      \"index\": {b.Index},");
            sb.AppendLine($"      \"timestamp\": \"{b.Timestamp.ToString("o", CultureInfo.InvariantCulture)}\",");
            sb.AppendLine($"      \"previousHash\": \"{b.PreviousHash}\",");
            sb.AppendLine("      \"transactions\": [");
            for (int j = 0; j < b.Transactions.Count; j++)
            {
                string comma = j < b.Transactions.Count - 1 ? "," : "";
                sb.AppendLine($"        \"{Escape(b.Transactions[j].ToString())}\"{comma}");
            }
            sb.AppendLine("      ],");
            sb.AppendLine($"      \"nonce\": {b.Nonce},");
            sb.AppendLine($"      \"hash\": \"{b.Hash}\"");
            sb.AppendLine(i < Blocks.Count - 1 ? "    }," : "    }");
        }

        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public void ExportJson(string path)
    {
        File.WriteAllText(path, ToJson());
    }

    /// Genera la pagina HTML interactivo: que muestra la cadena sellada y permite EDITAR cualquier dato
    /// para ver, en vivo, cómo se rompe la cadena. El navegador recalcula los hashes
    /// con la misma fórmula que C#, así que la validación es real, no simulada.
    public void ExportHtml(string path)
    {
        string json = ToJson();
        string html = HtmlVisor.Build(json);
        File.WriteAllText(path, html);
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
