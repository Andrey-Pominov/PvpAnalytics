using System.Text;
using System.Text.RegularExpressions;

namespace PvpAnalytics.Application.Logs;

/// <summary>
/// Parses Lua table format combat log files.
/// </summary>
public static partial class LuaTableParser
{
    /// <summary>
    /// Parses a Lua table format combat log stream and extracts match data.
    /// </summary>
    public static List<LuaMatchData> Parse(Stream stream)
    {
        using var reader =
            new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var content = reader.ReadToEnd();

        return ParseContent(content);
    }

    /// <summary>
    /// Parses Lua table content string and extracts match data.
    /// </summary>
    private static List<LuaMatchData> ParseContent(string content)
    {
        var matches = new List<LuaMatchData>();

        // Pattern to match a match block: { ["Logs"] = { ... }, ["StartTime"] = "...", etc. }
        // We'll use a simpler approach: find all match blocks using a compiled, source-generated regex.
        var regexMatches = MatchBlockRegex().Matches(content);

        foreach (Match match in regexMatches)
        {
            var matchData = new LuaMatchData();

            // Extract logs
            var logsContent = match.Groups[1].Value;
            matchData.Logs = ParseLogsArray(logsContent);

            // Extract metadata
            matchData.StartTime = match.Groups[2].Value.Trim();
            matchData.EndTime = match.Groups[3].Value.Trim();
            matchData.Zone = match.Groups[4].Value.Trim();
            matchData.Faction = match.Groups[5].Value.Trim();
            matchData.Mode = match.Groups[6].Value.Trim();

            matches.Add(matchData);
        }

        // If regex didn't work, try a more manual approach
        if (matches.Count == 0)
        {
            matches = ParseManually(content);
        }

        return matches;
    }

    private static List<string> ParseLogsArray(string logsContent)
    {
        var logs = new List<string>();

        // Pattern to match log entries: "HH:mm:ss - EVENT: details"
        var matches = LogEntryRegex().Matches(logsContent);

        foreach (Match match in matches)
        {
            var logLine = match.Groups[1].Value;
            if (!string.IsNullOrWhiteSpace(logLine) && logLine.Contains(" - "))
            {
                logs.Add(logLine);
            }
        }

        return logs;
    }

    private static List<LuaMatchData> ParseManually(string content)
    {
        var matches = new List<LuaMatchData>();
        var lines = content.Split('\n');
        var parserState = new ManualParserState();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.Trim();

            if (TryStartNewMatch(lines, i, trimmedLine, parserState))
                continue;

            if (parserState.CurrentMatch == null)
                continue;

            UpdateBraceDepth(line, parserState);
            ProcessLogsArray(line, trimmedLine, parserState);
            ExtractMetadataFields(line, parserState);

            // If we've closed the current match block (brace depth back to 0),
            // finalize this match and allow a new one to start later.
            if (parserState.BraceDepth <= 0 && parserState.CurrentMatch is { Logs.Count: > 0 })
            {
                matches.Add(parserState.CurrentMatch);
                parserState.CurrentMatch = null;
                parserState.InLogsArray = false;
                parserState.BraceDepth = 0;
            }
        }

        FinalizePendingMatch(parserState, matches);
        return matches;
    }

    private static bool TryStartNewMatch(string[] lines, int index, string trimmedLine, ManualParserState state)
    {
        if (trimmedLine != "{" || state.CurrentMatch != null || index == 0)
            return false;

        var prevLine = lines[index - 1].Trim();
        if (prevLine != "PvPAnalyticsDB = {" &&
            prevLine != "{" &&
            prevLine != "}," &&
            !prevLine.EndsWith('{'))
            return false;

        state.CurrentMatch = new LuaMatchData();
        state.BraceDepth = 1;
        return true;
    }

    private static void UpdateBraceDepth(string line, ManualParserState state)
    {
        var stringState = new StringState();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (IsQuote(c) && ProcessQuote(line, i, c, ref stringState)) continue;


            ProcessBrace(c, stringState.InsideString, state);
        }
    }

    private static bool IsQuote(char c)
    {
        return c == '"' || c == '\'';
    }

    private static int CountPrecedingBackslashes(string line, int position)
    {
        var count = 0;
        for (int j = position - 1; j >= 0 && line[j] == '\\'; j--)
        {
            count++;
        }

        return count;
    }

    private static bool IsQuoteEscaped(string line, int position)
    {
        var backslashCount = CountPrecedingBackslashes(line, position);
        return backslashCount % 2 == 1;
    }

    private static bool ProcessQuote(string line, int position, char quoteChar, ref StringState stringState)
    {
        if (IsQuoteEscaped(line, position))
        {
            return true; // Quote is escaped, ignore it
        }

        if (!stringState.InsideString)
        {
            // Entering a string
            stringState.InsideString = true;
            stringState.StringChar = quoteChar;
            return true;
        }

        if (quoteChar == stringState.StringChar)
        {
            // Exiting the string (matching quote type)
            stringState.InsideString = false;
            stringState.StringChar = '\0';
            return true;
        }

        // Inside a string but quote type doesn't match, it's part of the string content
        return true;
    }

    private static void ProcessBrace(char c, bool insideString, ManualParserState state)
    {
        if (insideString)
            return;

        if (c == '{')
        {
            state.BraceDepth++;
        }
        else if (c == '}')
        {
            state.BraceDepth--;
        }
    }

    private struct StringState
    {
        public bool InsideString { get; set; }
        public char StringChar { get; set; }
    }

    private static void ProcessLogsArray(string line, string trimmedLine, ManualParserState state)
    {
        if (trimmedLine.Contains("[\"Logs\"]") || trimmedLine.Contains("['Logs']"))
        {
            state.InLogsArray = true;
            return;
        }

        if (!state.InLogsArray)
            return;

        ExtractLogEntry(line, state);

        if (trimmedLine is "}," or "],")
        {
            state.InLogsArray = false;
        }
    }

    private static void ExtractLogEntry(string line, ManualParserState state)
    {
        // Guard against accessing CurrentMatch when null (can occur if InLogsArray becomes true before a match starts)
        if (state.CurrentMatch == null)
            return;

        var logMatch = MyRegex().Match(line);
        if (!logMatch.Success)
            return;

        var logLine = logMatch.Groups[1].Value.Replace("\\\"", "\"").Replace(@"\\", "\\");
        if (logLine.Contains(" - "))
        {
            state.CurrentMatch.Logs.Add(logLine);
        }
    }

    private static void ExtractMetadataFields(string line, ManualParserState state)
    {
        if (state.CurrentMatch == null)
            return;

        ExtractField(line, MyRegex1(), value => state.CurrentMatch.StartTime = value.Trim());
        ExtractField(line, MyRegex2(), value => state.CurrentMatch.EndTime = value.Trim());
        ExtractField(line, MyRegex3(), value => state.CurrentMatch.Zone = value.Trim());
        ExtractField(line, MyRegex4(), value => state.CurrentMatch.Faction = value.Trim());
        ExtractField(line, MyRegex5(), value => state.CurrentMatch.Mode = value.Trim());
    }

    private static void ExtractField(string line, Regex regex, Action<string> setter)
    {
        var match = regex.Match(line);
        if (match.Success)
        {
            setter(match.Groups[1].Value);
        }
    }

    private static void FinalizePendingMatch(ManualParserState state, List<LuaMatchData> matches)
    {
        if (state.CurrentMatch is { Logs.Count: > 0 })
        {
            matches.Add(state.CurrentMatch);
        }
    }

    private sealed class ManualParserState
    {
        public LuaMatchData? CurrentMatch { get; set; }
        public bool InLogsArray { get; set; }
        public int BraceDepth { get; set; }
    }

    [GeneratedRegex("""
                    \{[\s\S]*?\["Logs"\]\s*=\s*\{([\s\S]*?)\},[\s\S]*?\["StartTime"\]\s*=\s*"([^"]+)",[\s\S]*?\["EndTime"\]\s*=\s*"([^"]+)",[\s\S]*?\["Zone"\]\s*=\s*"([^"]+)",[\s\S]*?\["Faction"\]\s*=\s*"([^"]+)",[\s\S]*?\["Mode"\]\s*=\s*"([^"]+)",[\s\S]*?\}
                    """, RegexOptions.Multiline)]
    private static partial Regex MatchBlockRegex();

    [GeneratedRegex("""
                    "([^"\\]*(\\.[^"\\]*)*)"
                    """)]
    private static partial Regex MyRegex();

    [GeneratedRegex("""
                    "([^"]+)"
                    """)]
    private static partial Regex LogEntryRegex();

    [GeneratedRegex("""
                    \["StartTime"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex MyRegex1();

    [GeneratedRegex("""
                    \["EndTime"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex MyRegex2();

    [GeneratedRegex("""
                    \["Zone"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex MyRegex3();

    [GeneratedRegex("""
                    \["Faction"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex MyRegex4();

    [GeneratedRegex("""
                    \["Mode"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex MyRegex5();
}