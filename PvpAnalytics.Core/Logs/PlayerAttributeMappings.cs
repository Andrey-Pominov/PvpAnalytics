namespace PvpAnalytics.Core.Logs;

/// <summary>
/// Static mappings of spells and abilities to player attributes (Class, Spec, Faction/Race).
/// </summary>
public static class PlayerAttributeMappings
{
    /// <summary>
    /// Maps spell names to player classes. These are class-defining spells that only one class can use.
    /// </summary>
    public static readonly Dictionary<string, string> SpellToClass = new(StringComparer.OrdinalIgnoreCase)
    {
        // Mage spells
        { "Arcane Intellect", "Mage" },
        { "Polymorph", "Mage" },
        { "Blink", "Mage" },
        { "Counterspell", "Mage" },
        { "Ice Block", "Mage" },
        { "Combustion", "Mage" },
        { "Icy Veins", "Mage" },
        { "Time Warp", "Mage" },
        
        // Priest spells
        { "Power Word: Shield", "Priest" },
        { "Shadow Word: Pain", "Priest" },
        { "Dispel Magic", "Priest" },
        { "Psychic Scream", "Priest" },
        { "Mind Control", "Priest" },
        { "Voidform", "Priest" },
        { "Shadowfiend", "Priest" },
        
        // Warlock spells
        { "Soulstone", "Warlock" },
        { "Summon Imp", "Warlock" },
        { "Summon Voidwalker", "Warlock" },
        { "Summon Succubus", "Warlock" },
        { "Summon Felhunter", "Warlock" },
        { "Demonic Gateway", "Warlock" },
        { "Soulburn", "Warlock" },
        { "Chaos Bolt", "Warlock" },
        
        // Warrior spells
        { "Charge", "Warrior" },
        { "Shield Slam", "Warrior" },
        { "Execute", "Warrior" },
        { "Whirlwind", "Warrior" },
        { "Battle Shout", "Warrior" },
        { "Recklessness", "Warrior" },
        
        // Paladin spells
        { "Lay on Hands", "Paladin" },
        { "Divine Shield", "Paladin" },
        { "Hammer of Wrath", "Paladin" },
        { "Consecration", "Paladin" },
        { "Avenging Wrath", "Paladin" },
        { "Word of Glory", "Paladin" },
        
        // Hunter spells
        { "Hunter's Mark", "Hunter" },
        { "Aspect of the Cheetah", "Hunter" },
        { "Aspect of the Hawk", "Hunter" },
        { "Trap Launcher", "Hunter" },
        { "Freezing Trap", "Hunter" },
        { "Kill Command", "Hunter" },
        { "Bestial Wrath", "Hunter" },
        
        // Rogue spells
        { "Stealth", "Rogue" },
        { "Sap", "Rogue" },
        { "Vanish", "Rogue" },
        { "Kidney Shot", "Rogue" },
        { "Blade Flurry", "Rogue" },
        { "Shadow Dance", "Rogue" },
        { "Adrenaline Rush", "Rogue" },
        
        // Druid spells
        { "Bear Form", "Druid" },
        { "Cat Form", "Druid" },
        { "Travel Form", "Druid" },
        { "Moonkin Form", "Druid" },
        { "Rejuvenation", "Druid" },
        { "Entangling Roots", "Druid" },
        { "Innervate", "Druid" },
        { "Tranquility", "Druid" },
        
        // Shaman spells
        { "Lightning Bolt", "Shaman" },
        { "Chain Lightning", "Shaman" },
        { "Earth Shock", "Shaman" },
        { "Frost Shock", "Shaman" },
        { "Windfury Weapon", "Shaman" },
        { "Totemic Recall", "Shaman" },
        { "Spirit Walk", "Shaman" },
        
        // Death Knight spells
        { "Death Grip", "Death Knight" },
        { "Death Coil", "Death Knight" },
        { "Army of the Dead", "Death Knight" },
        { "Anti-Magic Shell", "Death Knight" },
        { "Unholy Frenzy", "Death Knight" },
        
        // Demon Hunter spells
        { "Metamorphosis", "Demon Hunter" },
        { "Chaos Strike", "Demon Hunter" },
        { "Fel Rush", "Demon Hunter" },
        { "Vengeful Retreat", "Demon Hunter" },
        { "Imprison", "Demon Hunter" },
        
        // Monk spells
        { "Roll", "Monk" },
        { "Flying Serpent Kick", "Monk" },
        { "Touch of Death", "Monk" },
        { "Storm, Earth, and Fire", "Monk" },
        { "Transcendence", "Monk" },
        
        // Evoker spells
        { "Living Flame", "Evoker" },
        { "Disintegrate", "Evoker" },
        { "Deep Breath", "Evoker" },
        { "Emerald Blossom", "Evoker" },
    };

    /// <summary>
    /// Maps spell names to player specializations. These are spec-defining spells.
    /// </summary>
    public static readonly Dictionary<string, string> SpellToSpec = new(StringComparer.OrdinalIgnoreCase)
    {
        // Mage specs
        { "Combustion", "Fire" },
        { "Icy Veins", "Frost" },
        { "Arcane Power", "Arcane" },
        { "Pyroblast", "Fire" },
        { "Ice Lance", "Frost" },
        { "Arcane Barrage", "Arcane" },
        
        // Priest specs
        { "Voidform", "Shadow" },
        { "Shadowfiend", "Shadow" },
        { "Penance", "Discipline" },
        { "Guardian Spirit", "Holy" },
        { "Lightwell", "Holy" },
        
        // Warlock specs
        { "Chaos Bolt", "Destruction" },
        { "Hand of Gul'dan", "Demonology" },
        { "Haunt", "Affliction" },
        { "Metamorphosis", "Demonology" },
        { "Drain Soul", "Affliction" },
        
        // Warrior specs
        { "Shield Slam", "Protection" },
        { "Recklessness", "Fury" },
        { "Colossus Smash", "Arms" },
        { "Raging Blow", "Fury" },
        { "Mortal Strike", "Arms" },
        
        // Paladin specs
        { "Word of Glory", "Protection" },
        { "Hammer of Wrath", "Retribution" },
        { "Light of Dawn", "Holy" },
        { "Shield of Vengeance", "Retribution" },
        { "Consecration", "Protection" },
        
        // Hunter specs
        { "Kill Command", "Beast Mastery" },
        { "Bestial Wrath", "Beast Mastery" },
        { "Explosive Shot", "Marksmanship" },
        { "Black Arrow", "Survival" },
        { "Carve", "Survival" },
        
        // Rogue specs
        { "Shadow Dance", "Subtlety" },
        { "Adrenaline Rush", "Outlaw" },
        { "Blade Flurry", "Outlaw" },
        { "Envenom", "Assassination" },
        { "Mutilate", "Assassination" },
        
        // Druid specs
        { "Bear Form", "Guardian" },
        { "Cat Form", "Feral" },
        { "Moonkin Form", "Balance" },
        { "Tranquility", "Restoration" },
        { "Innervate", "Restoration" },
        
        // Shaman specs
        { "Lava Burst", "Elemental" },
        { "Stormstrike", "Enhancement" },
        { "Riptide", "Restoration" },
        { "Chain Heal", "Restoration" },
        
        // Death Knight specs
        { "Frost Strike", "Frost" },
        { "Scourge Strike", "Unholy" },
        { "Heart Strike", "Blood" },
        { "Death Strike", "Blood" },
        
        // Demon Hunter specs
        { "Chaos Strike", "Havoc" },
        { "Soul Cleave", "Vengeance" },
        { "Immolation Aura", "Havoc" },
        
        // Monk specs
        { "Storm, Earth, and Fire", "Windwalker" },
        { "Touch of Death", "Windwalker" },
        { "Guard", "Brewmaster" },
        { "Soothing Mist", "Mistweaver" },
        
        // Evoker specs
        { "Disintegrate", "Devastation" },
        { "Emerald Blossom", "Preservation" },
        { "Deep Breath", "Devastation" },
    };

    /// <summary>
    /// Maps ability names to player races/factions. These are racial abilities.
    /// </summary>
    public static readonly Dictionary<string, string> AbilityToFaction = new(StringComparer.OrdinalIgnoreCase)
    {
        // Night Elf
        { "Shadowmeld", "Night Elf" },
        
        // Human
        { "Every Man for Himself", "Human" },
        { "Will to Survive", "Human" },
        
        // Dwarf
        { "Stoneform", "Dwarf" },
        
        // Gnome
        { "Escape Artist", "Gnome" },
        
        // Draenei
        { "Gift of the Naaru", "Draenei" },
        
        // Worgen
        { "Darkflight", "Worgen" },
        { "Two Forms", "Worgen" },
        
        // Orc
        { "Blood Fury", "Orc" },
        
        // Undead
        { "Will of the Forsaken", "Undead" },
        { "Cannibalize", "Undead" },
        
        // Tauren
        { "War Stomp", "Tauren" },
        
        // Troll
        { "Berserking", "Troll" },
        
        // Blood Elf
        { "Arcane Torrent", "Blood Elf" },
        
        // Goblin
        { "Rocket Jump", "Goblin" },
        { "Rocket Barrage", "Goblin" },
        
        // Pandaren
        { "Quaking Palm", "Pandaren" },
        
        // Nightborne
        { "Arcane Pulse", "Nightborne" },
        
        // Highmountain Tauren
        { "Bull Rush", "Highmountain Tauren" },
        
        // Lightforged Draenei
        { "Light's Judgment", "Lightforged Draenei" },
        
        // Void Elf
        { "Spatial Rift", "Void Elf" },
        
        // Dark Iron Dwarf
        { "Fireblood", "Dark Iron Dwarf" },
        
        // Mag'har Orc
        { "Ancestral Call", "Mag'har Orc" },
        
        // Kul Tiran
        { "Haymaker", "Kul Tiran" },
        
        // Zandalari Troll
        { "Regeneratin'", "Zandalari Troll" },
        
        // Mechagnome
        { "Hyper Organic Light Originator", "Mechagnome" },
        
        // Vulpera
        { "Make Camp", "Vulpera" },
        
        // Dracthyr
        { "Tail Swipe", "Dracthyr" },
    };

    /// <summary>
    /// Determines the player's class based on tracked spells.
    /// </summary>
    public static string? DetermineClass(HashSet<string> spells)
    {
        foreach (var spell in spells)
        {
            if (SpellToClass.TryGetValue(spell, out var playerClass))
            {
                return playerClass;
            }
        }
        return null;
    }

    /// <summary>
    /// Determines the player's spec based on tracked spells.
    /// </summary>
    public static string? DetermineSpec(HashSet<string> spells)
    {
        foreach (var spell in spells)
        {
            if (SpellToSpec.TryGetValue(spell, out var spec))
            {
                return spec;
            }
        }
        return null;
    }

    /// <summary>
    /// Determines the player's faction/race based on tracked abilities.
    /// </summary>
    public static string? DetermineFaction(HashSet<string> spells)
    {
        foreach (var spell in spells)
        {
            if (AbilityToFaction.TryGetValue(spell, out var faction))
            {
                return faction;
            }
        }
        return null;
    }
}

