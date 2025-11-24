# Spell Mappings Maintenance Process

## Target Expansion/Patch

**Current Target:** World of Warcraft: The War Within (Patch 11.0.x)  
**Last Verified:** November 2024  
**Location:** `PvpAnalytics.Core/Logs/PlayerAttributeMappings.cs`

## Overview

The `PlayerAttributeMappings` class contains static dictionaries that map spell names to player classes, specializations, and factions. These mappings are used to automatically detect player attributes from combat log entries.

**Key Mappings:**
- `SpellToClass` - Maps class-defining spells to class names
- `SpellToSpec` - Maps spec-defining spells to specialization names (with priority system)
- `AbilityToFaction` - Maps racial abilities to faction/race names

## Maintenance Triggers

Update these mappings when:

1. **New Expansion Release** - Major ability changes typically occur with new expansions
2. **Major Patch Release** - Class reworks or significant ability changes (e.g., 11.1, 11.2)
3. **Class Reworks** - When Blizzard announces class overhauls
4. **Spell Removal/Addition** - When class-defining spells are removed or new ones added
5. **Spec Changes** - When specializations are reworked or new specs are added

## Verification Process

### Step 1: Identify Changed Spells

1. Review Blizzard patch notes for the target expansion/patch
2. Check official class guides and ability lists
3. Test with actual combat logs from the target patch
4. Monitor for parsing errors or incorrect class/spec detection

### Step 2: Verify Spell Existence

For each spell in the mappings:

1. **Check Official Sources:**
   - [WoWHead](https://www.wowhead.com/) - Search spell name and verify it exists in target patch
   - [Icy Veins](https://www.icy-veins.com/wow/) - Class guides list current abilities
   - [Wowpedia](https://wowpedia.fandom.com/) - Historical spell information

2. **Test with Combat Logs:**
   - Upload combat logs from the target patch
   - Verify spells appear in logs with expected names
   - Check that class/spec detection works correctly

3. **Cross-Reference:**
   - Compare spell names in logs with mapping entries
   - Ensure case-insensitive matching works (mappings use `StringComparer.OrdinalIgnoreCase`)

### Step 3: Update Mappings

#### Adding New Spells

When a new class-defining spell is added:

```csharp
// Example: Adding a new Mage spell
{ "New Mage Spell", "Mage" },
```

**Guidelines:**
- Use exact spell name as it appears in combat logs
- Place in appropriate class section with other class spells
- Add comment if spell is expansion-specific

#### Removing Deprecated Spells

When a spell is removed from the game:

1. **Option A: Remove immediately** (if confirmed removed)
   ```csharp
   // Remove the entry entirely
   ```

2. **Option B: Mark as deprecated** (if unsure)
   ```csharp
   // Deprecated in Patch X.X - verify removal before deleting
   { "Old Spell", "Class" },
   ```

#### Updating Spec Mappings

When spec-defining spells change:

1. Update `SpellToSpec` dictionary
2. Update `SpellPriority` dictionary with appropriate priority:
   - **Priority 100**: Very definitive spec indicators (e.g., "Combustion" for Fire Mage)
   - **Priority 90**: Strong indicators
   - **Priority 80**: Moderate indicators
   - **Priority 50**: Default for unmapped spells

#### Updating Priority Values

If spell importance changes between patches:

1. Review priority values in `SpellPriority` dictionary
2. Adjust based on how definitive the spell is for a spec
3. Test priority-based spec detection with sample combat logs

### Step 4: Test Changes

1. **Unit Tests:**
   - Run existing tests in `PvpAnalytics.Tests`
   - Add new test cases for changed spells

2. **Integration Tests:**
   - Upload sample combat logs from target patch
   - Verify class/spec detection accuracy
   - Check that all expected spells are recognized

3. **Edge Cases:**
   - Test with logs containing removed spells (should gracefully handle)
   - Test with logs containing new spells (should detect correctly)
   - Test priority-based spec detection with multiple matching spells

### Step 5: Update Documentation

1. **Update Target Patch:**
   - Update the target expansion/patch in `PlayerAttributeMappings.cs` header
   - Update "Last Verified" date in this document

2. **Document Changes:**
   - Add changelog entry for significant updates
   - Note any breaking changes (removed spells, changed priorities)

3. **Update README:**
   - If spell maintenance process changes significantly, update README.md

## Example: Updating for a New Patch

### Scenario: Patch 11.1 Released

1. **Review Patch Notes:**
   - Check Blizzard patch notes for class changes
   - Identify any removed or renamed spells

2. **Test Current Mappings:**
   - Upload combat logs from Patch 11.1
   - Check for parsing errors or incorrect detections

3. **Update if Needed:**
   ```csharp
   // In PlayerAttributeMappings.cs header:
   // Target: World of Warcraft: The War Within (Patch 11.1.x)
   // Last Verified: [Current Date]
   ```

4. **Remove Deprecated Spells:**
   ```csharp
   // Remove if spell was removed in 11.1:
   // { "Removed Spell", "Class" },
   ```

5. **Add New Spells:**
   ```csharp
   // Add new class-defining spells:
   { "New Class Spell", "Class" },
   ```

6. **Test and Verify:**
   - Run test suite
   - Upload sample logs
   - Verify detection accuracy

## Spell Selection Criteria

### Class-Defining Spells (`SpellToClass`)

Include spells that:
- Are **unique to one class** (no other class can use them)
- Are **commonly used** in combat (not obscure or rarely-seen)
- Are **stable** (unlikely to be removed in near future)
- Have **clear class identity** (strongly associated with the class)

Examples:
- ✅ "Ice Block" - Unique to Mages, commonly used
- ✅ "Death Grip" - Unique to Death Knights, iconic ability
- ❌ "Fireball" - Generic damage spell, not class-defining
- ❌ "Auto Attack" - Too generic, used by all classes

### Spec-Defining Spells (`SpellToSpec`)

Include spells that:
- Are **unique to one specialization** within a class
- Are **core to the spec's rotation** or identity
- Have **high priority** if they're definitive spec indicators
- Are **commonly cast** in PvP scenarios

Examples:
- ✅ "Combustion" - Fire Mage only, very definitive (Priority 100)
- ✅ "Icy Veins" - Frost Mage only, very definitive (Priority 100)
- ✅ "Pyroblast" - Fire Mage, strong indicator (Priority 80)

### Racial Abilities (`AbilityToFaction`)

Include abilities that:
- Are **unique to a specific race**
- Are **commonly used** in PvP (not passive-only)
- Help **identify faction** (Alliance vs Horde)

## Troubleshooting

### Issue: Spell Not Detected

1. **Check Spell Name:**
   - Verify exact spelling matches combat log
   - Check for special characters or encoding issues
   - Ensure case-insensitive matching works

2. **Check Combat Log Format:**
   - Verify spell appears in logs with expected name
   - Check if spell name changed between patches

3. **Add Missing Spell:**
   - Add to appropriate dictionary
   - Test with sample logs

### Issue: Incorrect Class/Spec Detection

1. **Check Priority:**
   - Verify `SpellPriority` values are correct
   - Ensure higher-priority spells are detected first

2. **Check Multiple Matches:**
   - Review `DetermineSpec` logic for priority handling
   - Verify tie-breaking works correctly

3. **Check Spell Conflicts:**
   - Ensure no spell maps to multiple classes/specs incorrectly
   - Verify spell names are unique within dictionaries

## Resources

- **Official Sources:**
  - [Blizzard Patch Notes](https://worldofwarcraft.blizzard.com/en-us/news)
  - [WoWHead](https://www.wowhead.com/) - Spell database
  - [Icy Veins](https://www.icy-veins.com/wow/) - Class guides

- **Testing:**
  - Upload combat logs via `/api/logs/upload` endpoint
  - Check detected classes/specs in database
  - Review logs for parsing accuracy

## Changelog

### November 2024
- Initial documentation created
- Target: The War Within (Patch 11.0.x)
- Verified sample spells: Ice Block, Death Grip, Lay on Hands

