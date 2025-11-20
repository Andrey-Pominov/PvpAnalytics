using System.Text;
using System.Text.RegularExpressions;

namespace PvpAnalytics.Application.Logs;

/// <summary>
/// Parses Lua table format combat log files.
/// </summary>
public static class LuaTableParser
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
        var matchPattern = @"\{[\s\S]*?\[""Logs""\]\s*=\s*\{([\s\S]*?)\},[\s\S]*?\[""StartTime""\]\s*=\s*""([^""]+)"",[\s\S]*?\[""EndTime""\]\s*=\s*""([^""]+)"",[\s\S]*?\[""Zone""\]\s*=\s*""([^""]+)"",[\s\S]*?\[""Faction""\]\s*=\s*""([^""]+)"",[\s\S]*?\[""Mode""\]\s*=\s*""([^""]+)"",[\s\S]*?\}";
        
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
        var logPattern = @"""([^""]+)""";
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
        
        LuaMatchData? currentMatch = null;
        var inLogsArray = false;
        var braceDepth = 0;
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.Trim();
            
            // Detect start of a new match block (after PvPAnalyticsDB = {)
            if (trimmedLine == "{" && currentMatch == null && i > 0)
            {
                // Check if previous line suggests this is a match entry
                var prevLine = i > 0 ? lines[i - 1].Trim() : "";
                if (prevLine == "PvPAnalyticsDB = {" || prevLine == "{" || prevLine.EndsWith("{"))
                {
                    currentMatch = new LuaMatchData();
                    braceDepth = 1;
                    continue;
                }
            }
            
            if (currentMatch == null) continue;
            
            // Track brace depth (simple counting)
            foreach (var c in line)
            {
                if (c == '{') braceDepth++;
                if (c == '}') braceDepth--;
            }
            
            // Check for Logs array start
            if (trimmedLine.Contains("[\"Logs\"]") || trimmedLine.Contains("['Logs']"))
            {
                inLogsArray = true;
                continue;
            }
            
            // Collect log entries
            if (inLogsArray)
            {
                var logMatch = Regex.Match(line, @"""([^""\\]*(\\.[^""\\]*)*)""");
                if (logMatch.Success)
                {
                    var logLine = logMatch.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
                    if (logLine.Contains(" - "))
                    {
                        currentMatch.Logs.Add(logLine);
                    }
                }
                
                // Check if we're done with logs array
                if (trimmedLine == "}," || trimmedLine == "],")
                {
                    inLogsArray = false;
                }
            }
            
            // Extract StartTime
            var startTimeMatch = Regex.Match(line, @"\[""StartTime""\]\s*=\s*""([^""]+)""");
            if (startTimeMatch.Success)
            {
                currentMatch.StartTime = startTimeMatch.Groups[1].Value.Trim();
            }
            
            // Extract EndTime
            var endTimeMatch = Regex.Match(line, @"\[""EndTime""\]\s*=\s*""([^""]+)""");
            if (endTimeMatch.Success)
            {
                currentMatch.EndTime = endTimeMatch.Groups[1].Value.Trim();
            }
            
            // Extract Zone
            var zoneMatch = Regex.Match(line, @"\[""Zone""\]\s*=\s*""([^""]+)""");
            if (zoneMatch.Success)
            {
                currentMatch.Zone = zoneMatch.Groups[1].Value.Trim();
            }
            
            // Extract Faction
            var factionMatch = Regex.Match(line, @"\[""Faction""\]\s*=\s*""([^""]+)""");
            if (factionMatch.Success)
            {
                currentMatch.Faction = factionMatch.Groups[1].Value.Trim();
            }
            
            // Extract Mode
            var modeMatch = Regex.Match(line, @"\[""Mode""\]\s*=\s*""([^""]+)""");
            if (modeMatch.Success)
            {
                currentMatch.Mode = modeMatch.Groups[1].Value.Trim();
            }
            
            // Check if match block is complete (closing brace with depth back to 0 or 1)
            if (braceDepth <= 1 && currentMatch.Logs.Count > 0 && !string.IsNullOrEmpty(currentMatch.StartTime))
            {
                matches.Add(currentMatch);
                currentMatch = null;
                inLogsArray = false;
                braceDepth = 0;
            }
        }
        
        // Add last match if exists
        if (currentMatch != null && currentMatch.Logs.Count > 0)
        {
            matches.Add(currentMatch);
        }
        
        return matches;
    }
}

