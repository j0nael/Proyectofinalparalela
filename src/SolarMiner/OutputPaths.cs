namespace SolarMiner;

/// <summary>
/// Utilidad para guardar los archivos generados (CSV, JSON, HTML) en una carpeta
/// VISIBLE y fácil de encontrar, en vez de dejarlos enterrados dentro de
/// bin\Debug\net8.0. Busca la raíz del proyecto subiendo carpetas hasta encontrar
/// el archivo .csproj, y crea allí una carpeta "resultados".
/// </summary>
public static class OutputPaths
{
    /// <summary>
    /// Devuelve la ruta de la carpeta "resultados" en la raíz del proyecto,
    /// creándola si no existe. Si por alguna razón no encuentra la raíz, cae de
    /// vuelta a la carpeta del ejecutable (para no fallar nunca).
    /// </summary>
    public static string GetResultsDir()
    {
        string root = FindProjectRoot() ?? AppContext.BaseDirectory;
        string resultsDir = Path.Combine(root, "resultados");
        Directory.CreateDirectory(resultsDir);
        return resultsDir;
    }

    /// <summary>
    /// Sube carpeta por carpeta desde donde corre el programa hasta encontrar
    /// la que contiene el archivo del proyecto (SolarMiner.csproj). Esa es la raíz.
    /// </summary>
    private static string? FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            // ¿Hay un .csproj en esta carpeta? Entonces es la raíz del proyecto.
            if (dir.GetFiles("*.csproj").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
