using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SolarMiner;

/// Un bloque de la blockchain de la cooperativa solar.
///
/// Contiene un grupo de transacciones de energía y los datos que lo encadenan
/// con el bloque anterior. "Sellar" (minar) el bloque consiste en encontrar un
/// Nonce tal que el hash del bloque cumpla la condición de dificultad
/// (empezar con cierta cantidad de ceros).
public class Block
{
    ///Posición del bloque en la cadena (0 = bloque génesis).
    public int Index { get; set; }

    /// Momento en que se crea el bloque.
    public DateTime Timestamp { get; set; }

    /// Transacciones de energía que este bloque sella.
    public List<EnergyTransaction> Transactions { get; set; }

    ///Hash del bloque anterior. Esto es lo que "encadena" los bloques.
    public string PreviousHash { get; set; }

    /// El número comodín que se va cambiando durante el minado hasta que el hash
    /// del bloque cumple la dificultad. Es lo único del bloque que se modifica al minar.
    public long Nonce { get; set; }

    ///El hash resultante una vez sellado el bloque (la "huella" válida).
    public string Hash { get; set; } = string.Empty;

    /// Constructor. El 'timestamp' es opcional: si se pasa uno fijo, el bloque es
    /// DETERMINISTA (siempre plantea el mismo acertijo). Esto es clave para que la
    /// comparación secuencial-vs-paralela sea justa: ambas versiones deben resolver
    /// exactamente el mismo problema. Si no se pasa, se usa la hora actual.
    /// </summary>
    public Block(int index, List<EnergyTransaction> transactions, string previousHash,
                 DateTime? timestamp = null)
    {
        Index = index;
        Timestamp = timestamp ?? DateTime.UtcNow;
        Transactions = transactions;
        PreviousHash = previousHash;
        Nonce = 0;
    }

    // ------------------------------------------------------------------
    //  Contenido del bloque
    // ------------------------------------------------------------------
    //
    //  El contenido que se hashea es:  PREFIJO + nonce
    //  donde PREFIJO = índice | timestamp | hashPrevio | transacciones |
    //
    //  El PREFIJO es FIJO durante todo el minado (no depende del nonce). Por eso
    //  lo calculamos UNA sola vez y, en el bucle, solo cambiamos el nonce del final.

    /// Devuelve la parte FIJA del contenido del bloque (todo menos el nonce).
    /// Termina en '|' para que baste con pegarle el nonce al final.
    public string GetContentPrefix()
    {
        var sb = new StringBuilder();
        sb.Append(Index.ToString(CultureInfo.InvariantCulture));
        sb.Append('|');
        sb.Append(Timestamp.ToString("o", CultureInfo.InvariantCulture));
        sb.Append('|');
        sb.Append(PreviousHash);
        sb.Append('|');
        foreach (var tx in Transactions)
        {
            sb.Append(tx.ToString());
            sb.Append(';');
        }
        sb.Append('|');
        return sb.ToString();
    }

    /// La parte fija del contenido, ya convertida a bytes (para el minado rápido).
    public byte[] GetContentPrefixBytes()
    {
        return Encoding.UTF8.GetBytes(GetContentPrefix());
    }

    /// Contenido completo del bloque para un nonce dado: PREFIJO + nonce.
    /// Se usa para validar el bloque ya sellado (no en el bucle caliente).
    public string GetBlockContent(long nonce)
    {
        return GetContentPrefix() + nonce.ToString(CultureInfo.InvariantCulture);
    }

    // ------------------------------------------------------------------
    //  Cálculo del hash
    // ------------------------------------------------------------------

    /// Calcula el hash SHA-256 de un texto y lo devuelve como hexadecimal en
    /// minúsculas. Se usa para mostrar y validar el resultado.
    ///
    /// SHA-256 es la "función huella": misma entrada -> misma huella siempre;
    /// cambio mínimo en la entrada -> huella totalmente distinta e impredecible.
    /// Por eso la única forma de encontrar un hash válido es probar nonces.
    public static string ComputeHash(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        byte[] hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// Atajo: calcula el hash de ESTE bloque usando su nonce actual.
    public string ComputeHash()
    {
        return ComputeHash(GetBlockContent(Nonce));
    }
}
