namespace PvpAnalytics.Core.Logs;

public static class CombatLogEventTypes
{
    public const string ZoneChange = "ZONE_CHANGE";
    public const string ArenaMatchStart = "ARENA_MATCH_START";
    public const string CombatantInfo = "COMBATANT_INFO";

    public const string SwingDamage = "SWING_DAMAGE";
    public const string SwingMissed = "SWING_MISSED";

    public const string RangeDamage = "RANGE_DAMAGE";
    public const string SpellDamage = "SPELL_DAMAGE";
    public const string SpellMissed = "SPELL_MISSED";
    public const string SpellHeal = "SPELL_HEAL";
    public const string SpellPeriodicHeal = "SPELL_PERIODIC_HEAL";
    public const string SpellAuraApplied = "SPELL_AURA_APPLIED";
    public const string SpellAuraRemoved = "SPELL_AURA_REMOVED";
    public const string SpellAuraAppliedDose = "SPELL_AURA_APPLIED_DOSE";
    public const string SpellAuraRemovedDose = "SPELL_AURA_REMOVED_DOSE";
    public const string SpellCastStart = "SPELL_CAST_START";
    public const string SpellCastSuccess = "SPELL_CAST_SUCCESS";
    public const string SpellCastFailed = "SPELL_CAST_FAILED";
    public const string SpellEnergize = "SPELL_ENERGIZE";
    public const string SpellAbsorbed = "SPELL_ABSORBED";
    public const string SpellDispel = "SPELL_DISPEL";
    public const string SpellSummon = "SPELL_SUMMON";
    public const string SpellCreate = "SPELL_CREATE";
}


