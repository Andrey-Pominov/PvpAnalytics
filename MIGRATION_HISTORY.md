# Migration History Documentation

## Migration Chain

### 20251117115152_InitialCreate
- **Status**: Original state restored
- **Creates**:
  - `Matches` table with `MapName` column (string, required)
  - `Players` table with `Spec` column (string, required)
  - `MatchResults` table without `Spec` column
  - All other base tables and indexes

### 20251117120738_UpdateEntitiesRemoveUnusedFields
- **Status**: Fixed - no longer removes columns from InitialCreate
- **Changes**:
  - Adds `ArenaZone` column (integer, required, default 0) to `Matches` table
  - **Note**: Does NOT remove `MapName` or `Spec` from Players (these remain from InitialCreate)

### 20251117131927_AddArenaMatchIdAndSpec
- **Status**: Correct
- **Changes**:
  - Adds `ArenaMatchId` column (string, nullable) to `Matches` table
  - Adds `Spec` column (string, nullable) to `MatchResults` table

## Important Notes

1. **InitialCreate Restoration**: The `InitialCreate` migration has been restored to include `MapName` in `Matches` and `Spec` in `Players`, matching the original migration state.

2. **Migration Best Practices**: The `UpdateEntitiesRemoveUnusedFields` migration no longer removes columns that were created in `InitialCreate`. This maintains proper migration chain integrity.

3. **Database State**: All migrations currently show as "Pending", indicating the database was likely recreated. If the old migration (20250916152009) was applied to any database, you may need to manually sync the migration history.

4. **Future Migrations**: If `MapName` and `Spec` in Players need to be removed in the future, create a separate migration for that purpose, following the established pattern.

## Migration Order

1. `20251117115152_InitialCreate` - Creates base schema
2. `20251117120738_UpdateEntitiesRemoveUnusedFields` - Adds ArenaZone
3. `20251117131927_AddArenaMatchIdAndSpec` - Adds ArenaMatchId and Spec to MatchResults

