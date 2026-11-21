using OrbKit_Quest_Completer.CORE;
using System.Text.RegularExpressions;

namespace OrbKit_Quest_Completer;

public static partial class ExePicker
{
    [GeneratedRegex(@"[^a-zA-Z0-9 ]")]
    private static partial Regex NonAlphaNumSpaceRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpaceRegex();

    [GeneratedRegex(@"[^a-z0-9]", RegexOptions.IgnoreCase)]
    private static partial Regex LooseRegex();

    [GeneratedRegex(@"(^|[-_./ ])test([-_. ]|\.exe$)", RegexOptions.IgnoreCase)]
    private static partial Regex TestBuildRegex();

    public static string SanitizeName(string name)
    {
        string cleaned = MultiSpaceRegex().Replace(NonAlphaNumSpaceRegex().Replace(name ?? "", "").Trim(), " ");
        return string.IsNullOrWhiteSpace(cleaned) ? "Game" : cleaned;
    }

    public static ResolvedExecutable Pick(IEnumerable<GameExecutable>? executables, string appName)
    {
        string cleanName = SanitizeName(appName);
        string fallbackExe = $"{cleanName.Replace(" ", "")}.exe";

        var win32 = (executables ?? [])
            .Where(e => string.Equals(e.Os, "win32", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(e.Name))
            .Select(e => (
                IsLauncher: e.IsLauncher,
                Path: e.Name.Replace(">", "").Replace('\\', '/').Trim('/')
            ))
            .ToList();

        if (win32.Count == 0)
            return new ResolvedExecutable(fallbackExe, $"{cleanName}/{fallbackExe}");

        static string Rank((bool IsLauncher, string Path) entry)
        {
            string isTest = TestBuildRegex().IsMatch(entry.Path) ? "1" : "0";
            string isLauncher = (entry.IsLauncher || entry.Path.Contains("launcher", StringComparison.OrdinalIgnoreCase)) ? "1" : "0";
            string isNested = entry.Path.Contains('/') ? "1" : "0";
            return $"{isTest}{isLauncher}{isNested}{entry.Path.ToLowerInvariant()}";
        }

        string chosen = win32.OrderBy(Rank, StringComparer.Ordinal).First().Path;
        string[] parts = chosen.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string exeName = parts[^1];
        string[] dirs = parts[..^1];

        bool alreadyRooted = dirs.Length > 0 &&
            string.Equals(LooseRegex().Replace(dirs[0], ""), LooseRegex().Replace(cleanName, ""), StringComparison.OrdinalIgnoreCase);

        string relPath = alreadyRooted
            ? string.Join('/', parts)
            : string.Join('/', new[] { cleanName }.Concat(parts));

        return new ResolvedExecutable(exeName, relPath);
    }

    public static List<ResolvedExecutable> GetAllWin32(IEnumerable<GameExecutable>? executables, string appName)
    {
        string cleanName = SanitizeName(appName);
        string fallbackExe = $"{cleanName.Replace(" ", "")}.exe";

        var win32 = (executables ?? [])
            .Where(e => string.Equals(e.Os, "win32", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(e.Name))
            .Select(e => (
                IsLauncher: e.IsLauncher,
                Path: e.Name.Replace(">", "").Replace('\\', '/').Trim('/')
            ))
            .ToList();

        if (win32.Count == 0)
            return [new ResolvedExecutable(fallbackExe, $"{cleanName}/{fallbackExe}")];

        static string Rank((bool IsLauncher, string Path) entry)
        {
            string isTest = TestBuildRegex().IsMatch(entry.Path) ? "1" : "0";
            string isLauncher = (entry.IsLauncher || entry.Path.Contains("launcher", StringComparison.OrdinalIgnoreCase)) ? "1" : "0";
            string isNested = entry.Path.Contains('/') ? "1" : "0";
            return $"{isTest}{isLauncher}{isNested}{entry.Path.ToLowerInvariant()}";
        }

        var results = new List<ResolvedExecutable>();
        foreach (var entry in win32.OrderBy(Rank, StringComparer.Ordinal))
        {
            string[] parts = entry.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string exeName = parts[^1];
            string[] dirs = parts[..^1];

            bool alreadyRooted = dirs.Length > 0 &&
                string.Equals(LooseRegex().Replace(dirs[0], ""), LooseRegex().Replace(cleanName, ""), StringComparison.OrdinalIgnoreCase);

            string relPath = alreadyRooted
                ? string.Join('/', parts)
                : string.Join('/', new[] { cleanName }.Concat(parts));

            if (!results.Any(r => string.Equals(r.RelativePath, relPath, StringComparison.OrdinalIgnoreCase)))
                results.Add(new ResolvedExecutable(exeName, relPath));
        }

        return results;
    }
}