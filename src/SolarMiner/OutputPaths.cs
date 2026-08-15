namespace SolarMiner;

public static class OutputPaths
{
  
    public static string GetResultsDir()
    {
        string root = FindProjectRoot() ?? AppContext.BaseDirectory;
        string resultsDir = Path.Combine(root, "resultados");
        Directory.CreateDirectory(resultsDir);
        return resultsDir;
    }

    private static string? FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
           
            if (dir.GetFiles("*.csproj").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
