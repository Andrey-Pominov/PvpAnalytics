using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Enum;

namespace PvpAnalytics.Core.DTOs;

public sealed record MatchIngestionContext(
    ArenaZone ArenaZone,
    DateTime? Start,
    DateTime? End,
    HashSet<string> Participants,
    List<CombatLogEntry> Entries,
    Dictionary<string, Player> PlayersByKey,
    Dictionary<string, HashSet<string>> PlayerSpells,
    GameMode GameMode,
    string ArenaMatchId,
    string MapName);