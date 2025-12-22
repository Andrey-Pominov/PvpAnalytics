using System.Globalization;
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
        if (IsNewFormat(content))
        {
            return ParseNewFormat(content);
        }

        // Pattern to match a match block: { ["Logs"] = { ... }, ["StartTime"] = "...", etc. }
        // We'll use a simpler approach: find all match blocks using a compiled, source-generated regex.
        var regexMatches = MatchBlockRegex().Matches(content);

        var matches = regexMatches
            .Select(match =>
            {
                var groups = match.Groups;

                return new LuaMatchData
                {
                    Logs = ParseLogsArray(groups[1].Value),
                    StartTime = groups[2].Value.Trim(),
                    EndTime = groups[3].Value.Trim(),
                    Zone = groups[4].Value.Trim(),
                    Faction = groups[5].Value.Trim(),
                    Mode = groups[6].Value.Trim()
                };
            })
            .ToList();

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
            ProcessEventsArray(line, trimmedLine, parserState);
            ExtractMetadataFields(line, parserState);

            // If we've closed the current match block (brace depth back to 0),
            // finalize this match and allow a new one to start later.
            if (parserState is { BraceDepth: <= 0, CurrentMatch.Logs.Count: > 0 })
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
            prevLine != "[\"matches\"] = {" &&
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

    private static void ProcessEventsArray(string line, string trimmedLine, ManualParserState state)
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

    private static bool IsNewFormat(string content)
    {
        return content.Contains("[\"matches\"]") && content.Contains("[\"metadata\"]");
    }

    private static List<LuaMatchData> ParseNewFormat(string content)
    {
        var matches = new List<LuaMatchData>();
        var lines = content.Split('\n');
        var rootPlayers = ExtractRootPlayers(lines);
        var factionLookup = ExtractFactionMap(rootPlayers);
        var state = new NewFormatParserState
        {
            PlayerFactions = factionLookup,
            RootPlayers = rootPlayers
        };

        var inMatchesSection = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (!inMatchesSection)
            {
                if (trimmed.StartsWith("[\"matches\"]"))
                {
                    inMatchesSection = true;
                }

                continue;
            }

            if (TryStartNewMatchNew(trimmed, state))
                continue;

            if (state.CurrentMatch == null)
                continue;

            UpdateMatchBraceDepth(line, state);
            ProcessMatchPlayers(trimmed, line, state);
            ProcessEvents(line, trimmed, state);
            ProcessMetadata(line, trimmed, state);

            if (state is { MatchBraceDepth: <= 0, CurrentMatch.Logs.Count: > 0 })
            {
                FinalizeNewMatch(state);
                matches.Add(state.CurrentMatch!);
                state.Reset();
            }
        }

        if (state.CurrentMatch is { Logs.Count: > 0 })
        {
            FinalizeNewMatch(state);
            matches.Add(state.CurrentMatch);
        }

        // Add root players to all matches
        foreach (var match in matches)
        {
            match.Players.AddRange(rootPlayers);
        }

        return matches;
    }

    private static Dictionary<string, string> ExtractFactionMap(List<LuaPlayerData> players)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var player in players.Where(player => !string.IsNullOrEmpty(player.Faction)))
        {
            if (player.Faction != null) lookup[player.PlayerGuid] = player.Faction;
        }

        return lookup;
    }

    private static List<LuaPlayerData> ExtractRootPlayers(string[] lines)
    {
        var players = new List<LuaPlayerData>();
        var inPlayers = false;
        var playersDepth = 0;
        LuaPlayerData? currentPlayer = null;
        var playerBraceDepth = 0;
        var seenMatches = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            UpdateSeenMatches(trimmed, ref seenMatches);

            if (TryEnterPlayersSection(trimmed, ref inPlayers, ref playersDepth, seenMatches))
                continue;

            if (!inPlayers)
                continue;

            if (TryExitPlayersSection(trimmed, ref inPlayers, ref playersDepth, ref currentPlayer, players))
                continue;

            var playerMatch = PlayerIdRegex().Match(trimmed);
            if (TryStartNewPlayer(trimmed, playerMatch, ref currentPlayer, ref playerBraceDepth, players))
                continue;

            if (currentPlayer == null)
                continue;

            ProcessPlayerFieldLine(trimmed, playerMatch, currentPlayer, ref playerBraceDepth, players, ref currentPlayer);
        }

        FinalizePlayer(currentPlayer, players);
        return players;
    }

    private static void UpdateSeenMatches(string trimmed, ref bool seenMatches)
    {
        if (trimmed.StartsWith("[\"matches\"]"))
            seenMatches = true;
    }

    private static bool TryEnterPlayersSection(string trimmed, ref bool inPlayers, ref int playersDepth, bool seenMatches)
    {
        if (!inPlayers && trimmed.StartsWith("[\"players\"]") && !seenMatches)
        {
            inPlayers = true;
            playersDepth = CalculateBraceDelta(trimmed);
            return true;
        }
        return false;
    }

    private static bool TryExitPlayersSection(string trimmed, ref bool inPlayers, ref int playersDepth, ref LuaPlayerData? currentPlayer, List<LuaPlayerData> players)
    {
        playersDepth += CalculateBraceDelta(trimmed);
        if (playersDepth <= 0)
        {
            FinalizePlayer(currentPlayer, players);
            currentPlayer = null;
            inPlayers = false;
            return true;
        }
        return false;
    }

    private static bool TryStartNewPlayer(string trimmed, Match playerMatch, ref LuaPlayerData? currentPlayer, ref int playerBraceDepth, List<LuaPlayerData> players)
    {
        if (!playerMatch.Success)
            return false;

        FinalizePlayer(currentPlayer, players);
        currentPlayer = new LuaPlayerData
        {
            PlayerGuid = playerMatch.Groups[1].Value
        };
        playerBraceDepth = Math.Max(1, CalculateBraceDelta(trimmed));
        return true;
    }

    private static void ProcessPlayerFieldLine(string trimmed, Match playerMatch, LuaPlayerData currentPlayer, ref int playerBraceDepth, List<LuaPlayerData> players, ref LuaPlayerData? currentPlayerRef)
    {
        ExtractPlayerField(trimmed, currentPlayer);

        if (!playerMatch.Success)
            playerBraceDepth += CalculateBraceDelta(trimmed);

        if (playerBraceDepth <= 0)
        {
            FinalizePlayer(currentPlayerRef, players);
            currentPlayerRef = null;
        }
    }

    private static void FinalizePlayer(LuaPlayerData? currentPlayer, List<LuaPlayerData> players)
    {
        if (currentPlayer != null)
            players.Add(currentPlayer);
    }

    private static void ExtractPlayerField(string line, LuaPlayerData player)
    {
        ExtractField(line, NameRegex(), value => player.Name = value.Trim());
        ExtractField(line, RealmRegex(), value => player.Realm = value.Trim());
        ExtractField(line, ClassIdRegex(), value => player.ClassId = value.Trim());
        ExtractField(line, ClassRegex(), value => player.Class = value.Trim());
        ExtractField(line, SpecIdRegex(), value => ParseAndSetInt(value, val => player.SpecId = val));
        ExtractField(line, FactionRegex(), value => player.Faction = value.Trim());
        ExtractField(line, KdRatioRegex(), value => ParseAndSetDouble(value, val => player.KdRatio = val));
        ExtractField(line, LossesRegex(), value => ParseAndSetInt(value, val => player.Losses = val));
        ExtractField(line, WinsRegex(), value => ParseAndSetInt(value, val => player.Wins = val));
        ExtractField(line, MatchesPlayedRegex(), value => ParseAndSetInt(value, val => player.MatchesPlayed = val));
        ExtractField(line, TotalDamageRegex(), value => ParseAndSetLong(value, val => player.TotalDamage = val));
        ExtractField(line, TotalHealingRegex(), value => ParseAndSetLong(value, val => player.TotalHealing = val));
        ExtractField(line, InterruptsPerMatchRegex(), value => ParseAndSetDouble(value, val => player.InterruptsPerMatch = val));
    }

    private static void ParseAndSetInt(string value, Action<int> setter)
    {
        if (int.TryParse(value.Trim(), out var parsed))
            setter(parsed);
    }

    private static void ParseAndSetLong(string value, Action<long> setter)
    {
        if (long.TryParse(value.Trim(), out var parsed))
            setter(parsed);
    }

    private static void ParseAndSetDouble(string value, Action<double> setter)
    {
        if (double.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            setter(parsed);
    }

    private static bool TryStartNewMatchNew(string trimmedLine, NewFormatParserState state)
    {
        if (trimmedLine != "{" || state.CurrentMatch != null)
            return false;

        state.CurrentMatch = new LuaMatchData();
        state.MatchBraceDepth = 1;
        state.CurrentMatchPlayerIds.Clear();
        state.InEventsArray = false;
        state.InMetadata = false;
        state.InMatchPlayers = false;
        return true;
    }

    private static void UpdateMatchBraceDepth(string line, NewFormatParserState state)
    {
        state.MatchBraceDepth += CalculateBraceDelta(line);
    }

    private static void ProcessMatchPlayers(string trimmedLine, string rawLine, NewFormatParserState state)
    {
        if (trimmedLine.Contains("[\"players\"]"))
        {
            state.InMatchPlayers = true;
            state.MatchPlayersDepth = CalculateBraceDelta(rawLine);
            return;
        }

        if (!state.InMatchPlayers)
            return;

        state.MatchPlayersDepth += CalculateBraceDelta(rawLine);

        var playerMatch = PlayerIdRegex().Match(trimmedLine);
        if (playerMatch.Success)
        {
            var playerId = playerMatch.Groups[1].Value;
            state.CurrentMatchPlayerIds.Add(playerId);
        }

        if (state.MatchPlayersDepth <= 0)
        {
            state.InMatchPlayers = false;
        }
    }

    private static void ProcessEvents(string line, string trimmedLine, NewFormatParserState state)
    {
        if (trimmedLine.Contains("[\"events\"]"))
        {
            state.InEventsArray = true;
            state.EventsArrayDepth = CalculateBraceDelta(line);
            return;
        }

        if (!state.InEventsArray || state.CurrentMatch == null)
            return;

        var delta = CalculateBraceDelta(line);

        if (state.EventObjectDepth > 0)
        {
            state.CurrentEventLines.Add(line);
            state.EventObjectDepth += delta;
        }
        else if (trimmedLine == "{")
        {
            state.CurrentEventLines.Clear();
            state.CurrentEventLines.Add(line);
            state.EventObjectDepth = Math.Max(1, CalculateBraceDelta(line));
        }

        if (state.EventObjectDepth <= 0)
        {
            var logline = ConvertEventToLogLine(state.CurrentEventLines);
            if (!string.IsNullOrWhiteSpace(logline))
                state.CurrentMatch.Logs.Add(logline);
            state.EventObjectDepth = 0;
            state.CurrentEventLines.Clear();
        }

        state.EventsArrayDepth += delta;
        if (state.EventsArrayDepth <= 0)
        {
            state.InEventsArray = false;
            state.EventObjectDepth = 0;
            state.CurrentEventLines.Clear();
        }
    }

    private static void ProcessMetadata(string line, string trimmedLine, NewFormatParserState state)
    {
        if (trimmedLine.Contains("[\"metadata\"]"))
        {
            state.InMetadata = true;
            state.MetadataDepth = CalculateBraceDelta(line);
            return;
        }

        if (!state.InMetadata || state.CurrentMatch == null)
            return;

        ExtractField(line, DateRegex(), value => state.CurrentMatch.StartTime = value.Trim());
        ExtractField(line, EndTimeLowerRegex(), value => state.CurrentMatch.EndTime = value.Trim());
        ExtractField(line, MapRegex(), value => state.CurrentMatch.Zone = value.Trim());
        ExtractField(line, ModeLowerRegex(), value => state.CurrentMatch.Mode = value.Trim());

        state.MetadataDepth += CalculateBraceDelta(line);
        if (state.MetadataDepth <= 0)
        {
            state.InMetadata = false;
        }
    }

    private static void FinalizeNewMatch(NewFormatParserState state)
    {
        if (state.CurrentMatch == null)
            return;

        foreach (var playerId in state.CurrentMatchPlayerIds)
        {
            if (state.PlayerFactions.TryGetValue(playerId, out var faction))
            {
                state.CurrentMatch.Faction = faction;
                break;
            }
        }
    }

    private static int CalculateBraceDelta(string line)
    {
        var delta = 0;
        var stringState = new StringState();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (IsQuote(c) && ProcessQuote(line, i, c, ref stringState)) continue;

            if (stringState.InsideString)
                continue;

            if (c == '{')
                delta++;
            else if (c == '}')
                delta--;
        }

        return delta;
    }

    private static string ConvertEventToLogLine(List<string> eventLines)
    {
        if (eventLines.Count == 0)
            return string.Empty;

        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in eventLines)
        {
            var match = EventFieldRegex().Match(line.Trim());
            if (!match.Success)
                continue;

            var key = match.Groups[1].Value;
            var rawValue = match.Groups[2].Value.Trim().TrimEnd(',');
            data[key] = UnwrapValue(rawValue);
        }

        var time = data.TryGetValue("time", out var timeVal) ? FormatTime(timeVal) : "00:00:00";
        var type = data.TryGetValue("type", out var typeVal)
            ? typeVal
            : data.GetValueOrDefault("action", "EVENT");
        var spell = data.GetValueOrDefault("spellName", "Unknown");
        var source = data.TryGetValue("source", out var sourceVal)
            ? sourceVal
            : data.GetValueOrDefault("sourceGUID", "Unknown");
        var dest = data.TryGetValue("dest", out var destVal) ? destVal : string.Empty;

        var log = $"{time} - {type.ToUpperInvariant()}: {spell} from {source}";
        if (!string.IsNullOrWhiteSpace(dest))
        {
            log += $" to {dest}";
        }

        return log;
    }

    private static string UnwrapValue(string value)
    {
        if (value.StartsWith('\"') && value.EndsWith('\"') && value.Length >= 2)
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }

    private static string FormatTime(string value)
    {
        if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var timeSeconds))
            return value;

        try
        {
            var dto = DateTimeOffset.FromUnixTimeSeconds((long)Math.Floor(timeSeconds)).ToUniversalTime();
            return dto.ToString("HH:mm:ss");
        }
        catch
        {
            return timeSeconds.ToString("F0", CultureInfo.InvariantCulture);
        }
    }

    private sealed class ManualParserState
    {
        public LuaMatchData? CurrentMatch { get; set; }
        public bool InLogsArray { get; set; }
        public int BraceDepth { get; set; }
    }

    private sealed class NewFormatParserState
    {
        public LuaMatchData? CurrentMatch { get; set; }
        public bool InEventsArray { get; set; }
        public int EventsArrayDepth { get; set; }
        public int EventObjectDepth { get; set; }
        public List<string> CurrentEventLines { get; } = new();
        public bool InMetadata { get; set; }
        public int MetadataDepth { get; set; }
        public bool InMatchPlayers { get; set; }
        public int MatchPlayersDepth { get; set; }
        public int MatchBraceDepth { get; set; }
        public List<string> CurrentMatchPlayerIds { get; } = [];
        public Dictionary<string, string> PlayerFactions { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public List<LuaPlayerData> RootPlayers { get; set; } = [];

        public void Reset()
        {
            CurrentMatch = null;
            InEventsArray = false;
            EventsArrayDepth = 0;
            EventObjectDepth = 0;
            CurrentEventLines.Clear();
            InMetadata = false;
            MetadataDepth = 0;
            InMatchPlayers = false;
            MatchPlayersDepth = 0;
            MatchBraceDepth = 0;
            CurrentMatchPlayerIds.Clear();
        }
    }

    [GeneratedRegex("""
                    \{[\s\S]*?\["Logs"\]\s*=\s*\{([\s\S]*?)\},[\s\S]*?\["StartTime"\]\s*=\s*"([^"]+)",[\s\S]*?\["EndTime"\]\s*=\s*"([^"]+)",[\s\S]*?\["Zone"\]\s*=\s*"([^"]+)",[\s\S]*?\["Faction"\]\s*=\s*"([^"]+)",[\s\S]*?\["Mode"\]\s*=\s*"([^"]+)",[\s\S]*?\}
                    """, RegexOptions.Multiline)]
    private static partial Regex MatchBlockRegex();

    [GeneratedRegex("""
                    \["([^\"]+)"\]\s*=\s*(.+)
                    """)]
    private static partial Regex EventFieldRegex();

    [GeneratedRegex("""
                    \["(Player-[^"]+)"\]\s*=\s*\{
                    """)]
    private static partial Regex PlayerIdRegex();

    [GeneratedRegex("""
                    \["faction"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex FactionRegex();

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

    [GeneratedRegex("""
                    \["date"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex DateRegex();

    [GeneratedRegex("""
                    \["endTime"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex EndTimeLowerRegex();

    [GeneratedRegex("""
                    \["map"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex MapRegex();

    [GeneratedRegex("""
                    \["mode"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex ModeLowerRegex();

    [GeneratedRegex("""
                    \["name"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex NameRegex();

    [GeneratedRegex("""
                    \["realm"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex RealmRegex();

    [GeneratedRegex("""
                    \["classId"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex ClassIdRegex();

    [GeneratedRegex("""
                    \["class"\]\s*=\s*"([^"]+)"
                    """)]
    private static partial Regex ClassRegex();

    [GeneratedRegex("""
                    \["specId"\]\s*=\s*(\d+)
                    """)]
    private static partial Regex SpecIdRegex();

    [GeneratedRegex("""
                    \["kdratio"\]\s*=\s*([\d.]+)
                    """)]
    private static partial Regex KdRatioRegex();

    [GeneratedRegex("""
                    \["losses"\]\s*=\s*(\d+)
                    """)]
    private static partial Regex LossesRegex();

    [GeneratedRegex("""
                    \["wins"\]\s*=\s*(\d+)
                    """)]
    private static partial Regex WinsRegex();

    [GeneratedRegex("""
                    \["matchesPlayed"\]\s*=\s*(\d+)
                    """)]
    private static partial Regex MatchesPlayedRegex();

    [GeneratedRegex("""
                    \["totalDamage"\]\s*=\s*(\d+)
                    """)]
    private static partial Regex TotalDamageRegex();

    [GeneratedRegex("""
                    \["totalHealing"\]\s*=\s*(\d+)
                    """)]
    private static partial Regex TotalHealingRegex();

    [GeneratedRegex("""
                    \["interruptsPerMatch"\]\s*=\s*([\d.]+)
                    """)]
    private static partial Regex InterruptsPerMatchRegex();
}