namespace SolarMiner;

/// Representa un intercambio de energía dentro de la cooperativa solar.
/// Ejemplo: la casa "Casa-03" APORTÓ 4.5 kWh a la red comunitaria.
/// Estas transacciones son los "datos" que se agrupan en un bloque y se sellan.
public class EnergyTransaction
{
    /// Identificador de la casa/miembro (ej. "Casa-03").
    public string House { get; set; }

    /// Tipo de movimiento: "APORTA" (inyecta energía) o "CONSUME" (toma energía).
    public string Type { get; set; }

    /// Cantidad de energía en kilovatios-hora.
    public double KWh { get; set; }

    ///Momento en que ocurrió el intercambio.
    public DateTime Timestamp { get; set; }

    public EnergyTransaction(string house, string type, double kWh, DateTime timestamp)
    {
        House = house;
        Type = type;
        KWh = kWh;
        Timestamp = timestamp;
    }

    /// Representación en texto de la transacción. Se usa para calcular el hash del bloque:
    /// si alguien alterara una sola cifra aquí, el hash del bloque cambiaría por completo,
    /// y eso rompería la cadena. Esa es la propiedad que hace confiable a la blockchain.
    public override string ToString()
    {
        // Formato estable e invariante de cultura para que el hash sea reproducible
        // en cualquier computadora (evita que "4.5" se vuelva "4,5" en otra región).
        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0}|{1}|{2:F2}kWh|{3:yyyy-MM-ddTHH:mm:ss}",
            House, Type, KWh, Timestamp);
    }
}
