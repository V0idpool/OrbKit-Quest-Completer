using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OrbKit_Quest_Completer.CDPENGINE;

public sealed record ScriptValidationResult(
    bool IsValid,
    string Sha256,
    string Version,
    List<string> Errors,
    List<string> Warnings);

public static partial class CdpScriptValidator
{
    private const int MaxScriptBytes = 128 * 1024; // 128 KB cap

    [GeneratedRegex(@"version\s*:\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex VersionRegex();

    private static readonly (string Pattern, string Reason)[] BlockedPatterns =
    [
        (@"\bfetch\s*\(", "Calls global fetch() | external network requests are not allowed."),
        (@"\bXMLHttpRequest\b", "Uses XMLHttpRequest | external network requests are not allowed."),
        (@"\bWebSocket\b", "Opens a WebSocket | external connections are not allowed."),
        (@"\bsendBeacon\b", "Uses navigator.sendBeacon | external beacons are not allowed."),
        (@"\bEventSource\b", "Uses EventSource | external connections are not allowed."),
        (@"https?://", "Contains an absolute http(s):// URL | only relative Discord API paths are allowed."),
        (@"wss?://", "Contains a ws(s):// URL | external sockets are not allowed."),
        (@"\bgetToken\b", "References getToken | token access is blocked."),
        (@"\blocalStorage\b", "References localStorage | storage access is blocked."),
        (@"\bsessionStorage\b", "References sessionStorage | storage access is blocked."),
        (@"document\.cookie", "References document.cookie | cookie access is blocked."),
        (@"\bAuthorization\b", "References Authorization header | credential handling is blocked."),
        (@"webhooks", "References webhooks | exfiltration endpoint blocked."),
        (@"oauth2/authorize", "References OAuth2 authorization | blocked to protect account standing."),
        (@"discordsays", "References discordsays.com | achievement forgery endpoints are blocked."),
        (@"/billing|/payment", "References billing/payment endpoints | blocked."),
        (@"\beval\s*\(", "Uses eval() | dynamic code execution is blocked."),
        (@"\bFunction\s*\(", "Uses Function() constructor | dynamic code execution is blocked."),
        (@"\batob\s*\(", "Uses atob() | base64 payload decoding is blocked."),
        (@"fromCharCode", "Uses String.fromCharCode | obfuscated strings are blocked."),
        (@"\bimport\s*\(", "Uses dynamic import() | external module loading is blocked."),
        (@"document\.createElement", "Creates DOM elements | script/iframe injection is blocked."),
        (@"\bDiscordNative\b", "References DiscordNative | privileged Electron IPC is blocked."),
        (@"\bchild_process\b", "References child_process | blocked."),
        (@"\brequire\s*\(", "Calls require() | Node module loading is blocked.")
    ];

    private static readonly string[] RequiredSymbols =
    [
        "__DQC_ADAPTER",
        "fetchSnapshot",
        "enrollQuest",
        "sendVideoProgress"
    ];

    public static ScriptValidationResult ValidateStatic(string content)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(content))
        {
            errors.Add("Patch script file is empty.");
            return new ScriptValidationResult(false, string.Empty, "unknown", errors, warnings);
        }

        byte[] utf8 = Encoding.UTF8.GetBytes(content);
        string sha256 = Convert.ToHexString(SHA256.HashData(utf8));

        if (utf8.Length > MaxScriptBytes)
            errors.Add($"Patch script exceeds maximum allowed size ({utf8.Length / 1024} KB > {MaxScriptBytes / 1024} KB).");

        var verMatch = VersionRegex().Match(content);
        string version = verMatch.Success ? verMatch.Groups[1].Value : "unversioned";
        if (!verMatch.Success)
            warnings.Add("Patch script does not declare a version string.");

        string stripped = StripComments(content);

        foreach (string symbol in RequiredSymbols)
        {
            if (!stripped.Contains(symbol, StringComparison.Ordinal))
                errors.Add($"Missing required patch symbol: \"{symbol}\".");
        }

        foreach (var (pattern, reason) in BlockedPatterns)
        {
            if (Regex.IsMatch(stripped, pattern, RegexOptions.IgnoreCase))
                errors.Add($"Security check failed: {reason}");
        }

        if (!CheckBalancedDelimiters(content, out string balanceError))
            errors.Add($"Syntax check failed: {balanceError}");

        return new ScriptValidationResult(errors.Count == 0, sha256, version, errors, warnings);
    }

    private static string StripComments(string src)
    {
        var sb = new StringBuilder(src.Length);
        char quote = '\0';
        bool inLineComment = false;
        bool inBlockComment = false;

        for (int i = 0; i < src.Length; i++)
        {
            char c = src[i];
            char next = i + 1 < src.Length ? src[i + 1] : '\0';

            if (inLineComment)
            {
                if (c == '\n') { inLineComment = false; sb.Append('\n'); }
                continue;
            }

            if (inBlockComment)
            {
                if (c == '*' && next == '/') { inBlockComment = false; i++; }
                else if (c == '\n') sb.Append('\n');
                continue;
            }

            if (quote != '\0')
            {
                sb.Append(c);
                if (c == '\\' && i + 1 < src.Length) { sb.Append(src[++i]); continue; }
                if (c == quote) quote = '\0';
                continue;
            }

            if (c == '/' && next == '/') { inLineComment = true; i++; continue; }
            if (c == '/' && next == '*') { inBlockComment = true; i++; continue; }

            if (c is '\'' or '"' or '`') quote = c;
            sb.Append(c);
        }

        return sb.ToString();
    }

    private static bool CheckBalancedDelimiters(string src, out string error)
    {
        var stack = new Stack<(char Open, int Line)>();
        char quote = '\0';
        int quoteLine = 0;
        bool inLineComment = false;
        bool inBlockComment = false;
        int line = 1;

        for (int i = 0; i < src.Length; i++)
        {
            char c = src[i];
            char next = i + 1 < src.Length ? src[i + 1] : '\0';

            if (c == '\n') line++;

            if (inLineComment)
            {
                if (c == '\n') inLineComment = false;
                continue;
            }

            if (inBlockComment)
            {
                if (c == '*' && next == '/') { inBlockComment = false; i++; }
                continue;
            }

            if (quote != '\0')
            {
                if (c == '\\') { i++; continue; }
                if (c == quote) quote = '\0';
                continue;
            }

            if (c == '/' && next == '/') { inLineComment = true; i++; continue; }
            if (c == '/' && next == '*') { inBlockComment = true; i++; continue; }

            if (c is '\'' or '"' or '`')
            {
                quote = c;
                quoteLine = line;
                continue;
            }

            if (c is '(' or '{' or '[')
            {
                stack.Push((c, line));
            }
            else if (c is ')' or '}' or ']')
            {
                if (stack.Count == 0)
                {
                    error = $"Unexpected closing '{c}' on line {line}.";
                    return false;
                }

                var (open, openLine) = stack.Pop();
                bool match = (open == '(' && c == ')') || (open == '{' && c == '}') || (open == '[' && c == ']');
                if (!match)
                {
                    error = $"Mismatched '{open}' (line {openLine}) closed by '{c}' (line {line}).";
                    return false;
                }
            }
        }

        if (quote != '\0')
        {
            error = $"Unclosed string literal starting with {quote} on line {quoteLine}.";
            return false;
        }

        if (inBlockComment)
        {
            error = "Unclosed /* block comment.";
            return false;
        }

        if (stack.Count > 0)
        {
            var (open, openLine) = stack.Pop();
            error = $"Unclosed '{open}' opened on line {openLine}.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}