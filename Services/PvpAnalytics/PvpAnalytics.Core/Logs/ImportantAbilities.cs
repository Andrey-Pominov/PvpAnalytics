namespace PvpAnalytics.Core.Logs;

/// <summary>
/// Static helper class for identifying important abilities in combat logs.
/// Categorizes abilities as cooldowns/defensives or crowd control for match timeline analysis.
/// </summary>
public static class ImportantAbilities
{
    private static readonly HashSet<string> CooldownDefensiveAbilities = new(StringComparer.OrdinalIgnoreCase)
    {
        // Trinkets
        "Gladiator's Medallion",
        "Gladiator's Badge",
        "Adaptive Swarm",
        "Battlemaster's Determination",
        "Battlemaster's Resolve",
        "Relentless",
        "Unrelenting",
        "Adaptation",
        "PvP Trinket",
        
        // Warrior defensives
        "Shield Wall",
        "Last Stand",
        "Die by the Sword",
        "Spell Reflection",
        "Rallying Cry",
        "Avatar",
        "Recklessness",
        
        // Paladin defensives
        "Divine Shield",
        "Divine Protection",
        "Ardent Defender",
        "Guardian of Ancient Kings",
        "Lay on Hands",
        "Hand of Protection",
        "Aura Mastery",
        "Avenging Wrath",
        "Wings of Wrath",
        
        // Hunter defensives
        "Aspect of the Turtle",
        "Survival of the Fittest",
        "Exhilaration",
        "Feign Death",
        "Deterrence",
        "Trueshot",
        "Bestial Wrath",
        
        // Rogue defensives
        "Cloak of Shadows",
        "Evasion",
        "Feint",
        "Vanish",
        "Shadow Dance",
        "Adrenaline Rush",
        
        // Priest defensives
        "Power Word: Shield",
        "Pain Suppression",
        "Guardian Spirit",
        "Desperate Prayer",
        "Dispersion",
        "Void Shift",
        "Power Infusion",
        "Dark Archangel",
        
        // Shaman defensives
        "Astral Shift",
        "Shamanistic Rage",
        "Spirit Walk",
        "Ancestral Guidance",
        "Ancestral Protection Totem",
        "Spirit Link Totem",
        "Ascendance",
        "Earth Elemental",
        
        // Mage defensives
        "Ice Block",
        "Greater Invisibility",
        "Temporal Shield",
        "Alter Time",
        "Combustion",
        "Icy Veins",
        
        // Warlock defensives
        "Unending Resolve",
        "Dark Pact",
        "Soul Link",
        "Summon Infernal",
        "Summon Doomguard",
        
        // Monk defensives
        "Fortifying Brew",
        "Diffuse Magic",
        "Dampen Harm",
        "Touch of Karma",
        "Serenity",
        "Storm, Earth, and Fire",
        
        // Druid defensives
        "Barkskin",
        "Survival Instincts",
        "Ironbark",
        "Tranquility",
        "Innervate",
        "Incapacitating Roar",
        "Celestial Alignment",
        "Berserk",
        
        // Death Knight defensives
        "Icebound Fortitude",
        "Anti-Magic Shell",
        "Vampiric Blood",
        "Lichborne",
        "Dancing Rune Weapon",
        "Unholy Frenzy",
        
        // Demon Hunter defensives
        "Blur",
        "Darkness",
        "Metamorphosis",
        "Netherwalk",
        
        // Evoker defensives
        "Obsidian Scales",
        "Renewing Blaze",
        "Time Stop",
        "Time Dilation",
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
        "Kidney Shot",
        "Cheap Shot",
        "Gouge",
        "Shadowfury",
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
        "War Stomp",
        "Quaking Palm",
        "Spear Hand Strike",
        "Disrupting Shout",
        
        // Silences
        "Silence",
        "Counterspell",
        "Solar Beam",
        "Strangulate",
        "Disrupt",
        "Garrote - Silence",
        
        // Fears
        "Psychic Scream",
        "Howl of Terror",
        "Intimidating Roar",
        "Fear",
        "Horrify",
        "Dragon's Breath",
        
        // Polymorphs
        "Polymorph",
        "Hex",
        "Cyclone",
        "Hibernate",
        
        // Roots
        "Entangling Roots",
        "Frost Nova",
        "Freeze",
        "Freezing Trap",
        "Ice Trap",
        "Earthgrab",
        "Earthbind Totem",
        "Thunderstorm",
        "Frost Shock",
        
        // Slows
        "Hamstring",
        "Piercing Howl",
        "Crippling Poison",
        "Slow Fall",
        "Frostbolt",
        "Conflagrate",
        
        // Disorients
        "Blind",
        "Sap",
        "Repentance",
        "Shackle Undead",
        "Seduction",
        
        // Incapacitates
        "Incapacitating Roar",
        "Incapacitating Shout",
        "Paralysis",
        "Sleep",
        "Hibernate",
        "Ring of Frost",
        
        // Charms
        "Cyclone",
        "Mind Games",
        "Void Shift",
        
        // Other CC
        "Mind Control",
        "Death Grip",
        "Chains of Ice",
        "Grappling Hook",
        "Disorienting Roar",
    };

    private static readonly HashSet<string> AllImportantAbilities = new(StringComparer.OrdinalIgnoreCase)
    {
        // Combine both sets for quick lookup
    };

    static ImportantAbilities()
    {
        // Initialize combined set
        foreach (var ability in CooldownDefensiveAbilities)
        {
            AllImportantAbilities.Add(ability);
        }
        foreach (var ability in CrowdControlAbilities)
        {
            AllImportantAbilities.Add(ability);
        }
    }

    /// <summary>
    /// Checks if an ability is important (cooldown/defensive or crowd control).
    /// Sets out parameters to indicate the category.
    /// </summary>
    /// <param name="ability">The ability name to check. Can be null or empty.</param>
    /// <param name="isCooldown">True if the ability is a cooldown or defensive.</param>
    /// <param name="isCC">True if the ability is crowd control.</param>
    /// <returns>True if the ability is important, false otherwise (including null/empty).</returns>
    public static bool IsImportantAbility(string? ability, out bool isCooldown, out bool isCC)
    {
        // Guard against null/empty strings
        if (string.IsNullOrWhiteSpace(ability))
        {
            isCooldown = false;
            isCC = false;
            return false;
        }

        isCooldown = CooldownDefensiveAbilities.Contains(ability);
        isCC = CrowdControlAbilities.Contains(ability);
        return isCooldown || isCC;
    }

    /// <summary>
    /// Checks if an ability is a cooldown or defensive ability.
    /// </summary>
    /// <param name="ability">The ability name to check. Can be null or empty.</param>
    /// <returns>True if the ability is a cooldown/defensive, false otherwise (including null/empty).</returns>
    public static bool IsCooldownOrDefensive(string? ability)
    {
        // Guard against null/empty strings
        if (string.IsNullOrWhiteSpace(ability))
        {
            return false;
        }

        return CooldownDefensiveAbilities.Contains(ability);
    }

    /// <summary>
    /// Checks if an ability is crowd control.
    /// </summary>
    /// <param name="ability">The ability name to check. Can be null or empty.</param>
    /// <returns>True if the ability is crowd control, false otherwise (including null/empty).</returns>
    public static bool IsCrowdControl(string? ability)
    {
        // Guard against null/empty strings
        if (string.IsNullOrWhiteSpace(ability))
        {
            return false;
        }

        return CrowdControlAbilities.Contains(ability);
    }
}

