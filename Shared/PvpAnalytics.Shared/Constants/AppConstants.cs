namespace PvpAnalytics.Shared;

/// <summary>
/// Central application-wide constants for routes and configuration.
/// </summary>
public static class AppConstants
{
    public static class RouteConstants
    {
        // API base routes
        public const string KeyMomentsBase = "api/key-moments";
        public const string MatchupAnalyticsBase = "api/matchup-analytics";
        public const string MatchesBase = "api/matches";
        public const string TeamsBase = "api/teams";
        public const string TeamSynergyBase = "api/team-synergy";
        public const string TeamLeaderboardsBase = "api/team-leaderboards";
        public const string RivalsBase = "api/rivals";
        public const string FavoritesBase = "api/favorites";
        public const string DiscussionsBase = "api/discussions";
        public const string LogsBase = "api/logs";
        public const string PaymentBase = "api/payment";
        public const string OpponentScoutingBase = "api/opponent-scouting";
        public const string RatingProgressionBase = "api/rating-progression";
        public const string MetaAnalysisBase = "api/meta-analysis";
        public const string SessionAnalysisBase = "api/session-analysis";
    }

    public static class ConfigSectionNames
    {
        // High-level configuration sections
        public const string WowApi = "WowApi";
    }

    public static class ErrorMessages
    {
        public const string UserIdClaimNotFound = "User ID claim not found in token.";
        public const string WowApiClientIdRequired =
            "WowApi:ClientId is required. Please configure it in appsettings.json or environment variables.";

        public const string WowApiClientSecretRequired =
            "WowApi:ClientSecret is required. Please configure it in appsettings.json or environment variables.";
    }

    public static class AnalyticsThresholds
    {
        // Damage / healing thresholds used across analytics services
        public const int PotentialDeathDamage = 50_000;
        public const int DamageSpike = 100_000;
        public const int CriticalDamageSpike = 150_000;

        public const int MatchDetailHighDamage = 50_000;
        public const int MatchDetailHighHealing = 30_000;

        // Timeline filters
        public const int TimelineMinDamage = 10_000;
        public const int TimelineMinHealing = 5_000;
    }

    public static class WoWClass
    {
        public const string Priest = "Priest";
        public const string Warlock = "Warlock";
        public const string Mage = "Mage";
        public const string Warrior = "Warrior";
        public const string Paladin = "Paladin";
        public const string Hunter = "Hunter";
        public const string Rogue = "Rogue";
        public const string Druid = "Druid";
        public const string Shaman = "Shaman";
        public const string DeathKnight = "Death Knight";
        public const string DemonHunter = "Demon Hunter";
        public const string Evoker = "Evoker";
        public const string Monk = "Monk";
    }

    public static class WoWSpec
    {
        // Warrior
        public const string Protection = "Protection";
        public const string Fury = "Fury";
        public const string Arms = "Arms";

        // Paladin
        public const string Holy = "Holy";
        public const string Retribution = "Retribution";

        // Hunter
        public const string BeastMastery = "Beast Mastery";
        public const string Marksmanship = "Marksmanship";
        public const string Survival = "Survival";

        // Rogue
        public const string Assassination = "Assassination";
        public const string Outlaw = "Outlaw";
        public const string Subtlety = "Subtlety";

        // Druid
        public const string Guardian = "Guardian";
        public const string Feral = "Feral";
        public const string Balance = "Balance";
        public const string Restoration = "Restoration";

        // Shaman
        public const string Elemental = "Elemental";
        public const string Enhancement = "Enhancement";

        // Death Knight
        public const string Frost = "Frost";
        public const string Unholy = "Unholy";
        public const string Blood = "Blood";

        // Demon Hunter
        public const string Havoc = "Havoc";
        public const string Vengeance = "Vengeance";

        // Monk
        public const string Windwalker = "Windwalker";
        public const string Brewmaster = "Brewmaster";
        public const string Mistweaver = "Mistweaver";

        // Evoker
        public const string Devastation = "Devastation";
        public const string Preservation = "Preservation";
    }
}


