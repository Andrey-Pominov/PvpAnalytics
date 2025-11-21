namespace PvpAnalytics.Core.Logs;

/// <summary>
/// Helper class to identify important abilities (major cooldowns, defensives, crowd control) in combat logs.
/// </summary>
public static class ImportantAbilities
{
    // Major cooldowns and defensives (trinkets, major defensives per class)
    private static readonly HashSet<string> CooldownDefensiveAbilities = new(StringComparer.OrdinalIgnoreCase)
    {
        // Trinkets
        "Gladiator's Medallion",
        "Gladiator's Badge",
        "Adaptive Swarm",
        "Battlemaster's Determination",
        "Battlemaster's Resolve",
        
        // Warrior
        "Shield Wall",
        "Last Stand",
        "Die by the Sword",
        "Spell Reflection",
        "Rallying Cry",
        "Avatar",
        "Recklessness",
        
        // Paladin
        "Divine Shield",
        "Divine Protection",
        "Ardent Defender",
        "Guardian of Ancient Kings",
        "Aura Mastery",
        "Avenging Wrath",
        "Wings of Wrath",
        
        // Hunter
        "Aspect of the Turtle",
        "Survival of the Fittest",
        "Exhilaration",
        "Trueshot",
        "Bestial Wrath",
        
        // Rogue
        "Cloak of Shadows",
        "Evasion",
        "Feint",
        "Vanish",
        "Shadow Dance",
        "Adrenaline Rush",
        
        // Priest
        "Power Word: Shield",
        "Pain Suppression",
        "Guardian Spirit",
        "Desperate Prayer",
        "Void Shift",
        "Power Infusion",
        "Dark Archangel",
        
        // Shaman
        "Astral Shift",
        "Ancestral Protection Totem",
        "Spirit Link Totem",
        "Ascendance",
        "Earth Elemental",
        
        // Mage
        "Ice Block",
        "Greater Invisibility",
        "Temporal Shield",
        "Alter Time",
        "Combustion",
        "Icy Veins",
        
        // Warlock
        "Unending Resolve",
        "Dark Pact",
        "Soul Link",
        "Summon Infernal",
        "Summon Doomguard",
        
        // Monk
        "Fortifying Brew",
        "Diffuse Magic",
        "Dampen Harm",
        "Touch of Karma",
        "Serenity",
        "Storm, Earth, and Fire",
        
        // Druid
        "Barkskin",
        "Survival Instincts",
        "Ironbark",
        "Incapacitating Roar",
        "Innervate",
        "Celestial Alignment",
        "Berserk",
        
        // Death Knight
        "Icebound Fortitude",
        "Anti-Magic Shell",
        "Lichborne",
        "Dancing Rune Weapon",
        "Unholy Frenzy",
        
        // Demon Hunter
        "Blur",
        "Darkness",
        "Metamorphosis",
        "Netherwalk",
        
        // Evoker
        "Obsidian Scales",
        "Renewing Blaze",
        "Time Stop",
        "Dragonrage",
    };

    // Crowd control abilities (stuns, silences, fears, etc.)
    private static readonly HashSet<string> CrowdControlAbilities = new(StringComparer.OrdinalIgnoreCase)
    {
        // Stuns
        "Hammer of Justice",
        "Blinding Light",
        "Storm Bolt",
        "Intimidating Shout",
        "Charge",
        "Intercept",
        "Concussive Shot",
        "Scatter Shot",
        "Freezing Trap",
        "Kidney Shot",
        "Cheap Shot",
        "Gouge",
        "Shadowfury",
        "Chaos Bolt",
        "Fists of Fury",
        "Leg Sweep",
        "Mighty Bash",
        "Maim",
        "Bash",
        "Asphyxiate",
        "Mind Freeze",
        "Gnaw",
        "Fel Eruption",
        "Chaos Nova",
        
        // Silences
        "Silence",
        "Counterspell",
        "Solar Beam",
        "Strangulate",
        "Disrupt",
        
        // Fears
        "Psychic Scream",
        "Howl of Terror",
        "Intimidating Roar",
        "Fear",
        "Horrify",
        
        // Polymorphs
        "Polymorph",
        "Hex",
        "Cyclone",
        "Hibernate",
        
        // Roots
        "Entangling Roots",
        "Frost Nova",
        "Freeze",
        "Hamstring",
        "Piercing Howl",
        "Earthbind Totem",
        
        // Disorients
        "Blind",
        "Sap",
        "Repentance",
        "Shackle Undead",
        
        // Incapacitates
        "Incapacitating Roar",
        "Incapacitating Shout",
        "Paralysis",
        "Sleep",
        
        // Other CC
        "Mind Control",
        "Death Grip",
        "Grappling Hook",
        "Disorienting Roar",
    };

    /// <summary>
    /// Determines if an ability is important (cooldown/defensive or crowd control).
    /// </summary>
    /// <param name="ability">The ability name to check.</param>
    /// <param name="isCooldown">True if the ability is a major cooldown or defensive.</param>
    /// <param name="isCC">True if the ability is crowd control.</param>
    /// <returns>True if the ability is important (cooldown/defensive or CC).</returns>
    public static bool IsImportantAbility(string ability, out bool isCooldown, out bool isCC)
    {
        isCooldown = CooldownDefensiveAbilities.Contains(ability);
        isCC = CrowdControlAbilities.Contains(ability);
        return isCooldown || isCC;
    }

    /// <summary>
    /// Checks if an ability is a major cooldown or defensive.
    /// </summary>
    public static bool IsCooldownOrDefensive(string ability)
    {
        return CooldownDefensiveAbilities.Contains(ability);
    }

    /// <summary>
    /// Checks if an ability is crowd control.
    /// </summary>
    public static bool IsCrowdControl(string ability)
    {
        return CrowdControlAbilities.Contains(ability);
    }
}

