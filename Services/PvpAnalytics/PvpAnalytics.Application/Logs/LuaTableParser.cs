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
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var content = reader.ReadToEnd();
        
        return ParseContent(content);
    }

    /// <summary>
    /// Parses Lua table content string and extracts match data.
    /// </summary>
    public static List<LuaMatchData> ParseContent(string content)
    {
        var matches = new List<LuaMatchData>();
        
        // Pattern to match a match block: { ["Logs"] = { ... }, ["StartTime"] = "...", etc. }
        // We'll use a simpler approach: find all match blocks
        var matchPattern = """\{[\s\S]*?\["Logs"\]\s*=\s*\{([\s\S]*?)\},[\s\S]*?\["StartTime"\]\s*=\s*"([^"]+)",[\s\S]*?\["EndTime"\]\s*=\s*"([^"]+)",[\s\S]*?\["Zone"\]\s*=\s*"([^"]+)",[\s\S]*?\["Faction"\]\s*=\s*"([^"]+)",[\s\S]*?\["Mode"\]\s*=\s*"([^"]+)",[\s\S]*?\}""";
        
        var regex = new Regex(matchPattern, RegexOptions.Multiline);
        var regexMatches = regex.Matches(content);
        
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
        var logPattern = """
                         "([^"]+)"
                         """;
        var regex = new Regex(logPattern);
        var matches = regex.Matches(logsContent);
        
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
            
            if (TryFinalizeMatch(parserState, matches))
                continue;
        }
        
        FinalizePendingMatch(parserState, matches);
        return matches;
    }

    private static bool TryStartNewMatch(string[] lines, int index, string trimmedLine, ManualParserState state)
    {
        if (trimmedLine != "{" || state.CurrentMatch != null || index == 0)
            return false;

        var prevLine = lines[index - 1].Trim();
        if (prevLine != "PvPAnalyticsDB = {" && prevLine != "{" && !prevLine.EndsWith("{"))
            return false;

        state.CurrentMatch = new LuaMatchData();
        state.BraceDepth = 1;
        return true;
    }

    private static void UpdateBraceDepth(string line, ManualParserState state)
    {
        var insideString = false;
        var stringChar = '\0'; // Tracks which quote type we're inside (' or ")

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            // Check if we're encountering a quote
            if (c == '"' || c == '\'')
            {
                // Check if this quote is escaped by counting preceding backslashes
                var backslashCount = 0;
                for (int j = i - 1; j >= 0 && line[j] == '\\'; j--)
                {
                    backslashCount++;
                }

                // If odd number of backslashes, the quote is escaped (ignore it)
                if (backslashCount % 2 == 1)
                    continue;

                // Toggle string state
                if (!insideString)
                {
                    // Entering a string
                    insideString = true;
                    stringChar = c;
                }
                else if (c == stringChar)
                {
                    // Exiting the string (matching quote type)
                    insideString = false;
                    stringChar = '\0';
                }
                // If inside a string but quote type doesn't match, it's part of the string content
                continue;
            }

            // Only count braces when outside of strings
            if (!insideString)
            {
                if (c == '{') state.BraceDepth++;
                if (c == '}') state.BraceDepth--;
            }
        }
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
        ExtractField(line, MyRegex1(), value => state.CurrentMatch!.StartTime = value.Trim());
        ExtractField(line, MyRegex2(), value => state.CurrentMatch!.EndTime = value.Trim());
        ExtractField(line, MyRegex3(), value => state.CurrentMatch!.Zone = value.Trim());
        ExtractField(line, MyRegex4(), value => state.CurrentMatch!.Faction = value.Trim());
        ExtractField(line, MyRegex5(), value => state.CurrentMatch!.Mode = value.Trim());
    }

    private static void ExtractField(string line, Regex regex, Action<string> setter)
    {
        var match = regex.Match(line);
        if (match.Success)
        {
            setter(match.Groups[1].Value);
        }
    }

    private static bool TryFinalizeMatch(ManualParserState state, List<LuaMatchData> matches)
    {
        if (state.BraceDepth > 1 || state.CurrentMatch!.Logs.Count <= 0 || string.IsNullOrEmpty(state.CurrentMatch.StartTime))
            return false;

        matches.Add(state.CurrentMatch);
        state.Reset();
        return true;
    }

    private static void FinalizePendingMatch(ManualParserState state, List<LuaMatchData> matches)
    {
        if (state.CurrentMatch is { Logs.Count: > 0 })
        {
            matches.Add(state.CurrentMatch);
        }
    }

    private class ManualParserState
    {
        public LuaMatchData? CurrentMatch { get; set; }
        public bool InLogsArray { get; set; }
        public int BraceDepth { get; set; }

        public void Reset()
        {
            CurrentMatch = null;
            InLogsArray = false;
            BraceDepth = 0;
        }
    }

    [GeneratedRegex("""
                    "([^"\\]*(\\.[^"\\]*)*)"
                    """)]
    private static partial Regex MyRegex();
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

