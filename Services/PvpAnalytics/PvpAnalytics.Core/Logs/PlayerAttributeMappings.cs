using PvpAnalytics.Shared;

namespace PvpAnalytics.Core.Logs;

/// <summary>
/// Static mappings of spells and abilities to player attributes (Class, Spec, Faction/Race).
/// 
/// <para><strong>Target Expansion/Patch:</strong></para>
/// <para>These mappings target World of Warcraft: The War Within (Patch 11.0.x) as of November 2024.</para>
/// <para>Spell mappings are verified against the current expansion but may become outdated when Blizzard changes class abilities in future patches.</para>
/// 
/// <para><strong>Maintenance:</strong></para>
/// <para>See SPELL_MAINTENANCE.md for the process to update these mappings when Blizzard releases new patches or expansions.</para>
/// </summary>
public static class PlayerAttributeMappings
{
   

    /// <summary>
    /// Maps spell names to player classes. These are class-defining spells that only one class can use.
    /// </summary>
    private static readonly Dictionary<string, string> SpellToClass = new(StringComparer.OrdinalIgnoreCase)
    {
        // Mage spells
        { "Arcane Intellect", AppConstants.WoWClass.Mage },
        { "Polymorph", AppConstants.WoWClass.Mage },
        { "Blink", AppConstants.WoWClass.Mage },
        { "Counterspell", AppConstants.WoWClass.Mage },
        { "Ice Block", AppConstants.WoWClass.Mage },
        { "Combustion", AppConstants.WoWClass.Mage },
        { "Icy Veins", AppConstants.WoWClass.Mage },
        { "Time Warp", AppConstants.WoWClass.Mage },
        
        // Priest spells
        { "Power Word: Shield", AppConstants.WoWClass.Priest },
        { "Shadow Word: Pain", AppConstants.WoWClass.Priest },
        { "Dispel Magic", AppConstants.WoWClass.Priest },
        { "Psychic Scream",AppConstants.WoWClass.Priest },
        { "Mind Control", AppConstants.WoWClass.Priest },
        { "Voidform", AppConstants.WoWClass.Priest },
        { "Shadowfiend", AppConstants.WoWClass.Priest },
        
        // Warlock spells
        { "Soulstone", AppConstants.WoWClass.Warlock },
        { "Summon Imp", AppConstants.WoWClass.Warlock },
        { "Summon Voidwalker", AppConstants.WoWClass.Warlock },
        { "Summon Succubus", AppConstants.WoWClass.Warlock },
        { "Summon Felhunter", AppConstants.WoWClass.Warlock },
        { "Demonic Gateway", AppConstants.WoWClass.Warlock },
        { "Soulburn", AppConstants.WoWClass.Warlock },
        { "Chaos Bolt", AppConstants.WoWClass.Warlock },
        
        // Warrior spells
        { "Charge", AppConstants.WoWClass.Warrior },
        { "Shield Slam", AppConstants.WoWClass.Warrior },
        { "Execute", AppConstants.WoWClass.Warrior },
        { "Whirlwind", AppConstants.WoWClass.Warrior },
        { "Battle Shout", AppConstants.WoWClass.Warrior },
        { "Recklessness", AppConstants.WoWClass.Warrior },
        
        // Paladin spells
        { "Lay on Hands", AppConstants.WoWClass.Paladin },
        { "Divine Shield", AppConstants.WoWClass.Paladin },
        { "Hammer of Wrath", AppConstants.WoWClass.Paladin },
        { "Consecration", AppConstants.WoWClass.Paladin },
        { "Avenging Wrath", AppConstants.WoWClass.Paladin },
        { "Word of Glory", AppConstants.WoWClass.Paladin },
        
        // Hunter spells
        { "Hunter's Mark", AppConstants.WoWClass.Hunter },
        { "Aspect of the Cheetah", AppConstants.WoWClass.Hunter },
        { "Aspect of the Hawk", AppConstants.WoWClass.Hunter },
        { "Trap Launcher", AppConstants.WoWClass.Hunter },
        { "Freezing Trap", AppConstants.WoWClass.Hunter },
        { "Kill Command", AppConstants.WoWClass.Hunter },
        { "Bestial Wrath", AppConstants.WoWClass.Hunter },
        
        // Rogue spells
        { "Stealth", AppConstants.WoWClass.Rogue },
        { "Sap", AppConstants.WoWClass.Rogue },
        { "Vanish", AppConstants.WoWClass.Rogue },
        { "Kidney Shot", AppConstants.WoWClass.Rogue },
        { "Blade Flurry", AppConstants.WoWClass.Rogue },
        { "Shadow Dance", AppConstants.WoWClass.Rogue },
        { "Adrenaline Rush", AppConstants.WoWClass.Rogue },
        
        // Druid spells
        { "Bear Form", AppConstants.WoWClass.Druid },
        { "Cat Form", AppConstants.WoWClass.Druid },
        { "Travel Form", AppConstants.WoWClass.Druid },
        { "Moonkin Form", AppConstants.WoWClass.Druid },
        { "Rejuvenation", AppConstants.WoWClass.Druid },
        { "Entangling Roots", AppConstants.WoWClass.Druid },
        { "Innervate", AppConstants.WoWClass.Druid },
        { "Tranquility", AppConstants.WoWClass.Druid },
        
        // Shaman spells
        { "Lightning Bolt", AppConstants.WoWClass.Shaman },
        { "Chain Lightning", AppConstants.WoWClass.Shaman },
        { "Earth Shock", AppConstants.WoWClass.Shaman },
        { "Frost Shock", AppConstants.WoWClass.Shaman },
        { "Windfury Weapon", AppConstants.WoWClass.Shaman },
        { "Totemic Recall", AppConstants.WoWClass.Shaman },
        { "Spirit Walk", AppConstants.WoWClass.Shaman },
        
        // Death Knight spells
        { "Death Grip", AppConstants.WoWClass.DeathKnight },
        { "Death Coil", AppConstants.WoWClass.DeathKnight },
        { "Army of the Dead",AppConstants.WoWClass.DeathKnight },
        { "Anti-Magic Shell", AppConstants.WoWClass.DeathKnight },
        { "Unholy Frenzy", AppConstants.WoWClass.DeathKnight },
        
        // Demon Hunter spells
        { "Metamorphosis", AppConstants.WoWClass.DemonHunter },
        { "Chaos Strike", AppConstants.WoWClass.DemonHunter },
        { "Fel Rush", AppConstants.WoWClass.DemonHunter },
        { "Vengeful Retreat", AppConstants.WoWClass.DemonHunter },
        { "Imprison", AppConstants.WoWClass.DemonHunter },
        
        // Monk spells
        { "Roll", AppConstants.WoWClass.Monk },
        { "Flying Serpent Kick", AppConstants.WoWClass.Monk },
        { "Touch of Death", AppConstants.WoWClass.Monk },
        { "Storm, Earth, and Fire", AppConstants.WoWClass.Monk },
        { "Transcendence", AppConstants.WoWClass.Monk },
        
        // Evoker spells
        { "Living Flame", AppConstants.WoWClass.Evoker },
        { "Disintegrate", AppConstants.WoWClass.Evoker },
        { "Deep Breath", AppConstants.WoWClass.Evoker },
        { "Emerald Blossom", AppConstants.WoWClass.Evoker },
    };

    /// <summary>
    /// Maps spell names to player specializations. These are spec-defining spells.
    /// </summary>
    private static readonly Dictionary<string, string> SpellToSpec = new(StringComparer.OrdinalIgnoreCase)
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
        { "Drain Soul", "Affliction" },
        
        // Warrior specs
        { "Shield Slam", AppConstants.WoWSpec.Protection },
        { "Recklessness", AppConstants.WoWSpec.Fury },
        { "Colossus Smash", AppConstants.WoWSpec.Arms },
        { "Raging Blow", AppConstants.WoWSpec.Fury },
        { "Mortal Strike", AppConstants.WoWSpec.Arms },
        
        // Paladin specs
        { "Word of Glory", AppConstants.WoWSpec.Protection },
        { "Hammer of Wrath", AppConstants.WoWSpec.Retribution },
        { "Light of Dawn", AppConstants.WoWSpec.Holy },
        { "Shield of Vengeance", AppConstants.WoWSpec.Retribution },
        { "Consecration", AppConstants.WoWSpec.Protection },
        
        // Hunter specs
        { "Kill Command", AppConstants.WoWSpec.BeastMastery },
        { "Bestial Wrath", AppConstants.WoWSpec.BeastMastery },
        { "Explosive Shot", AppConstants.WoWSpec.Marksmanship },
        { "Black Arrow", AppConstants.WoWSpec.Survival },
        { "Carve", AppConstants.WoWSpec.Survival },
        
        // Rogue specs
        { "Shadow Dance", AppConstants.WoWSpec.Subtlety },
        { "Adrenaline Rush", AppConstants.WoWSpec.Outlaw },
        { "Blade Flurry", AppConstants.WoWSpec.Outlaw },
        { "Envenom", AppConstants.WoWSpec.Assassination },
        { "Mutilate", AppConstants.WoWSpec.Assassination },
        
        // Druid specs
        { "Bear Form", AppConstants.WoWSpec.Guardian },
        { "Cat Form", AppConstants.WoWSpec.Feral },
        { "Moonkin Form", AppConstants.WoWSpec.Balance },
        { "Tranquility", AppConstants.WoWSpec.Restoration },
        { "Innervate", AppConstants.WoWSpec.Restoration },
        
        // Shaman specs
        { "Lava Burst", AppConstants.WoWSpec.Elemental },
        { "Stormstrike", AppConstants.WoWSpec.Enhancement },
        { "Riptide", AppConstants.WoWSpec.Restoration },
        { "Chain Heal", AppConstants.WoWSpec.Restoration },
        
        // Death Knight specs
        { "Frost Strike", AppConstants.WoWSpec.Frost },
        { "Scourge Strike", AppConstants.WoWSpec.Unholy },
        { "Heart Strike", AppConstants.WoWSpec.Blood },
        { "Death Strike", AppConstants.WoWSpec.Blood },
        
        // Demon Hunter specs
        { "Chaos Strike", AppConstants.WoWSpec.Havoc },
        { "Soul Cleave", AppConstants.WoWSpec.Vengeance },
        { "Immolation Aura", AppConstants.WoWSpec.Havoc },
        
        // Monk specs
        { "Storm, Earth, and Fire", AppConstants.WoWSpec.Windwalker },
        { "Touch of Death", AppConstants.WoWSpec.Windwalker },
        { "Guard", AppConstants.WoWSpec.Brewmaster },
        { "Soothing Mist", AppConstants.WoWSpec.Mistweaver },
        
        // Evoker specs
        { "Disintegrate", AppConstants.WoWSpec.Devastation },
        { "Emerald Blossom", AppConstants.WoWSpec.Preservation },
        { "Deep Breath", AppConstants.WoWSpec.Devastation },
    };

    /// <summary>
    /// Maps ability names to player races/factions. These are racial abilities.
    /// </summary>
    private static readonly Dictionary<string, string> AbilityToFaction = new(StringComparer.OrdinalIgnoreCase)
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
    /// High-confidence class-defining spells that are extremely unique to their class.
    /// These spells are checked first to ensure accurate class detection.
    /// </summary>
    private static readonly HashSet<string> HighConfidenceClassSpells = new(StringComparer.OrdinalIgnoreCase)
    {
        // Mage - very unique defensive ability
        "Ice Block",
        
        // Death Knight - iconic class-defining ability
        "Death Grip",
        
        // Paladin - unique powerful heal
        "Lay on Hands",
        
        // Demon Hunter - unique transformation
        "Metamorphosis",
        
        // Druid - form abilities are very unique
        "Bear Form",
        "Cat Form",
        "Moonkin Form",
        
        // Rogue - stealth is very Rogue-defining
        "Stealth",
        "Vanish",
        
        // Warlock - unique pet summoning
        "Summon Imp",
        "Summon Voidwalker",
        "Summon Succubus",
        "Summon Felhunter",
        "Demonic Gateway",
        
        // Hunter - unique pet abilities
        "Bestial Wrath",
        "Kill Command",
        
        // Priest - unique abilities
        "Mind Control",
        "Voidform",
        
        // Warrior - unique charge ability
        "Charge",
        
        // Shaman - unique totem ability
        "Totemic Recall",
        
        // Monk - unique movement
        "Roll",
        "Flying Serpent Kick",
        
        // Evoker - unique class abilities
        "Deep Breath",
        "Disintegrate",
    };

    /// <summary>
    /// Determines the player's class based on tracked spells using priority-aware lookup.
    /// First scans for high-confidence class-defining spells, then falls back to any matching class.
    /// </summary>
    public static string? DetermineClass(HashSet<string> spells)
    {
        if (spells.Count == 0)
            return null;

        // First pass: Check for high-confidence class-defining spells
        foreach (var spell in spells)
        {
            if (HighConfidenceClassSpells.Contains(spell) && 
                SpellToClass.TryGetValue(spell, out var highConfidenceClass))
            {
                return highConfidenceClass;
            }
        }

        // Second pass: Fall back to any matching class from SpellToClass
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
    /// Maps spell names to priority values for spec determination.
    /// Higher priority values indicate more definitive spec indicators.
    /// </summary>
    private static readonly Dictionary<string, int> SpellPriority = new(StringComparer.OrdinalIgnoreCase)
    {
        // Mage specs - high priority for definitive spells
        { "Combustion", 100 },      // Fire - very definitive
        { "Icy Veins", 100 },        // Frost - very definitive
        { "Arcane Power", 100 },      // Arcane - very definitive
        { "Pyroblast", 80 },          // Fire - strong indicator
        { "Ice Lance", 80 },          // Frost - strong indicator
        { "Arcane Barrage", 80 },     // Arcane - strong indicator
        
        // Priest specs
        { "Voidform", 100 },          // Shadow - very definitive
        { "Shadowfiend", 90 },        // Shadow - strong indicator
        { "Penance", 100 },           // Discipline - very definitive
        { "Guardian Spirit", 100 },   // Holy - very definitive
        { "Lightwell", 90 },          // Holy - strong indicator
        
        // Warlock specs
        { "Chaos Bolt", 100 },        // Destruction - very definitive
        { "Hand of Gul'dan", 100 },   // Demonology - very definitive
        { "Haunt", 100 },             // Affliction - very definitive
        { "Drain Soul", 80 },         // Affliction - strong indicator
        
        // Warrior specs
        { "Shield Slam", 100 },       // Protection - very definitive
        { "Recklessness", 100 },      // Fury - very definitive
        { "Colossus Smash", 100 },    // Arms - very definitive
        { "Raging Blow", 90 },        // Fury - strong indicator
        { "Mortal Strike", 90 },      // Arms - strong indicator
        
        // Paladin specs
        { "Word of Glory", 100 },     // Protection - very definitive
        { "Hammer of Wrath", 100 },   // Retribution - very definitive
        { "Light of Dawn", 100 },     // Holy - very definitive
        { "Shield of Vengeance", 90 }, // Retribution - strong indicator
        { "Consecration", 80 },       // Protection - moderate indicator
        
        // Hunter specs
        { "Kill Command", 100 },      // Beast Mastery - very definitive
        { "Bestial Wrath", 100 },     // Beast Mastery - very definitive
        { "Explosive Shot", 100 },    // Marksmanship - very definitive
        { "Black Arrow", 100 },       // Survival - very definitive
        { "Carve", 90 },              // Survival - strong indicator
        
        // Rogue specs
        { "Shadow Dance", 100 },      // Subtlety - very definitive
        { "Adrenaline Rush", 100 },   // Outlaw - very definitive
        { "Blade Flurry", 100 },      // Outlaw - very definitive
        { "Envenom", 100 },           // Assassination - very definitive
        { "Mutilate", 90 },           // Assassination - strong indicator
        
        // Druid specs
        { "Bear Form", 100 },         // Guardian - very definitive
        { "Cat Form", 100 },          // Feral - very definitive
        { "Moonkin Form", 100 },     // Balance - very definitive
        { "Tranquility", 100 },      // Restoration - very definitive
        { "Innervate", 90 },          // Restoration - strong indicator
        
        // Shaman specs
        { "Lava Burst", 100 },        // Elemental - very definitive
        { "Stormstrike", 100 },       // Enhancement - very definitive
        { "Riptide", 100 },          // Restoration - very definitive
        { "Chain Heal", 90 },         // Restoration - strong indicator
        
        // Death Knight specs
        { "Frost Strike", 100 },     // Frost - very definitive
        { "Scourge Strike", 100 },   // Unholy - very definitive
        { "Heart Strike", 100 },      // Blood - very definitive
        { "Death Strike", 90 },       // Blood - strong indicator
        
        // Demon Hunter specs
        { "Chaos Strike", 100 },      // Havoc - very definitive
        { "Soul Cleave", 100 },      // Vengeance - very definitive
        { "Immolation Aura", 90 },    // Havoc - strong indicator
        
        // Monk specs
        { "Storm, Earth, and Fire", 100 }, // Windwalker - very definitive
        { "Touch of Death", 100 },    // Windwalker - very definitive
        { "Guard", 100 },             // Brewmaster - very definitive
        { "Soothing Mist", 100 },     // Mistweaver - very definitive
        
        // Evoker specs
        { "Disintegrate", 100 },      // Devastation - very definitive
        { "Emerald Blossom", 100 },   // Preservation - very definitive
        { "Deep Breath", 90 },         // Devastation - strong indicator
    };

    /// <summary>
    /// Determines the player's spec based on tracked spells using priority-based matching.
    /// Returns the spec whose matching spell has the highest priority, breaking ties deterministically.
    /// </summary>
    public static string? DetermineSpec(HashSet<string> spells)
    {
        if (spells.Count == 0)
            return null;

        // Collect all candidate specs with their priorities
        var candidates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var spell in spells)
        {
            if (SpellToSpec.TryGetValue(spell, out var spec))
            {
                // Get priority for this spell (default to 50 if not in priority map)
                var priority = SpellPriority.GetValueOrDefault(spell, 50);
                
                // Keep the highest priority for each spec
                if (!candidates.TryGetValue(spec, out var existingPriority) || priority > existingPriority)
                {
                    candidates[spec] = priority;
                }
            }
        }

        if (candidates.Count == 0)
            return null;

        // Return the spec with the highest priority
        // If there's a tie, use stable ordering (alphabetical by spec name)
        var bestCandidate = candidates
            .OrderByDescending(kvp => kvp.Value)  // Highest priority first
            .ThenBy(kvp => kvp.Key)                // Then alphabetical for tie-breaking
            .First();

        return bestCandidate.Key;
    }

    /// <summary>
    /// Determines the player's faction/race based on tracked abilities.
    /// </summary>
    public static string? DetermineFaction(HashSet<string> spells)
    {
        if (spells.Count == 0)
            return null;

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