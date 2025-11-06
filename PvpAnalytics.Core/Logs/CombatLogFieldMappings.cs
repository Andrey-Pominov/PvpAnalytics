namespace PvpAnalytics.Core.Logs;

public static class CombatLogFieldMappings
{
    // Common indices for many SPELL_* / RANGE_* events based on standard WoW log format
    // Indexing refers to the CSV AFTER the timestamp and two-space separator; fields[0] is the event name
    public static class Common
    {
        public const int Event = 0;
        public const int SourceGuid = 1;
        public const int SourceName = 2;
        public const int TargetGuid = 5;
        public const int TargetName = 6;
        public const int SpellId = 9;
        public const int SpellName = 10;
    }

    public static class SpellDamage
    {
        public const int Amount = 12;
    }

    public static class SpellHeal
    {
        public const int Amount = 12;
    }

    public static class SpellAbsorbed
    {
        // Typical ABSORBED amount column for SPELL_ABSORBED events
        public const int Amount = 15;
    }

    public static class ZoneChange
    {
        public const int ZoneId = 1;   // numeric
        public const int ZoneName = 2; // localized name
    }

    public static class SwingDamage
    {
        public const int Amount = 9;
    }
}


