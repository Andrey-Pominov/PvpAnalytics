namespace PvpAnalytics.Core.Enum;

/// <summary>
/// Represents all known World of Warcraft Specializations with their official API ID values.
/// These IDs are used for various API lookups (Blizzard, Raider.IO, etc.).
/// </summary>
public enum WoWSpecialization
{
    // --- Death Knight (6) ---
    Blood = 250,
    FrostDK = 251,
    Unholy = 252,

    // --- Demon Hunter (12) ---
    Havoc = 577,
    Vengeance = 581,

    // --- Druid (11) ---
    Balance = 102,
    Feral = 103,
    Guardian = 104,
    RestorationDruid = 105, 

    // --- Evoker (13) ---
    Devastation = 1467,
    Preservation = 1468,
    Augmentation = 1473,

    // --- Hunter (3) ---
    BeastMastery = 253,
    Marksmanship = 254,
    Survival = 255,

    // --- Mage (8) ---
    Arcane = 62,
    Fire = 63,
    FrostMage = 64, 

    // --- Monk (10) ---
    Brewmaster = 268,
    Mistweaver = 270,
    Windwalker = 269,

    // --- Paladin (2) ---
    HolyPaladin = 65, 
    ProtectionPaladin = 66,
    Retribution = 70,

    // --- Priest (5) ---
    Discipline = 256,
    HolyPriest = 257,
    Shadow = 258,

    // --- Rogue (4) ---
    Assassination = 259,
    Outlaw = 260,
    Subtlety = 261,

    // --- Shaman (7) ---
    Elemental = 262,
    Enhancement = 263,
    RestorationShaman = 264,

    // --- Warlock (9) ---
    Affliction = 265,
    Demonology = 266,
    Destruction = 267,

    // --- Warrior (1) ---
    Arms = 71,
    Fury = 72,
    ProtectionWarrior = 73
}