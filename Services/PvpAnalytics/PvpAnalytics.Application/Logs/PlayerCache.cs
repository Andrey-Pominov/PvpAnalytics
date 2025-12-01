using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Repositories;

namespace PvpAnalytics.Application.Logs;

/// <summary>
/// Manages player caching and batching during combat log ingestion.
/// Tracks pending creates, updates, and provides batch lookup capabilities.
/// </summary>
public class PlayerCache(IRepository<Player> playerRepo)
{
    private readonly Dictionary<string, Player> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PendingPlayer> _pendingCreates = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<Player> _pendingUpdates = new();
    private readonly HashSet<string> _lookedUpNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a player from cache or returns null if not found.
    /// </summary>
    public Player? GetCached(string name)
    {
        return _cache.GetValueOrDefault(name);
    }

    /// <summary>
    /// Adds a player to the cache.
    /// </summary>
    public void AddToCache(string name, Player player)
    {
        _cache[name] = player;
    }

    /// <summary>
    /// Gets or adds a pending player creation.
    /// </summary>
    public PendingPlayer GetOrAddPending(string name, string realm)
    {
        if (_pendingCreates.TryGetValue(name, out var pending)) return pending;
        pending = new PendingPlayer { Name = name, Realm = realm };
        _pendingCreates[name] = pending;
        return pending;
    }

    /// <summary>
    /// Marks a player for update (will be batch updated later).
    /// </summary>
    public void MarkForUpdate(Player player)
    {
        if (player.Id > 0) // Only update existing players
        {
            _pendingUpdates.Add(player);
        }
    }

    /// <summary>
    /// Gets all pending create operations.
    /// </summary>
    public IReadOnlyDictionary<string, PendingPlayer> GetPendingCreates() => _pendingCreates;

    /// <summary>
    /// Gets all pending update operations.
    /// </summary>
    public IReadOnlyCollection<Player> GetPendingUpdates() => _pendingUpdates;

    /// <summary>
    /// Gets names that need to be looked up from the database.
    /// </summary>
    private HashSet<string> GetNamesToLookup()
    {
        var namesToLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Add pending creates that haven't been looked up
        foreach (var name in _pendingCreates.Keys.Where(name => !_cache.ContainsKey(name) && !_lookedUpNames.Contains(name)))
        {
            namesToLookup.Add(name);
        }

        return namesToLookup;
    }

    /// <summary>
    /// Performs batch lookup of players from the database.
    /// </summary>
    public async Task BatchLookupAsync(CancellationToken ct = default)
    {
        var namesToLookup = GetNamesToLookup();
        if (namesToLookup.Count == 0)
            return;

        // Batch lookup by querying for all names at once
        // Note: EF Core doesn't support IN clause directly, so we'll need to query in chunks
        const int chunkSize = 100;
        var nameList = namesToLookup.ToList();

        for (int i = 0; i < nameList.Count; i += chunkSize)
        {
            var chunk = nameList.Skip(i).Take(chunkSize).ToList();
            
            // Query for players matching any name in the chunk
            var existingPlayers = await playerRepo.ListAsync(
                p => chunk.Contains(p.Name), 
                ct);

            foreach (var player in existingPlayers)
            {
                _cache[player.Name] = player;
                _lookedUpNames.Add(player.Name);
                
                // Remove from pending creates if it exists
                _pendingCreates.Remove(player.Name);
            }
        }

        // Mark all queried names as looked up
        foreach (var name in namesToLookup)
        {
            _lookedUpNames.Add(name);
        }
    }

    /// <summary>
    /// Persists all pending creates and updates in batches.
    /// </summary>
    public async Task BatchPersistAsync(CancellationToken ct = default)
    {
        // Create new players
        var playersToCreate = (from kvp in _pendingCreates
        select kvp.Value
        into pending
        where !string.IsNullOrWhiteSpace(pending.Realm)
        where !_cache.ContainsKey(pending.Name)
        select new Player
        {
            Name = pending.Name,
            Realm = pending.Realm,
            Class = string.Empty,
            Faction = string.Empty,
            Spec = string.Empty,
            MatchResults = null,
            SourceCombatLogs = null,
            TargetCombatLogs = null
        }).ToList();

        if (playersToCreate.Count > 0)
        {
            await playerRepo.AddRangeAsync(playersToCreate, true, ct);
            
            // Add created players to cache
            foreach (var player in playersToCreate)
            {
                _cache[player.Name] = player;
            }
        }

        // Update existing players
        if (_pendingUpdates.Count > 0)
        {
            await playerRepo.UpdateRangeAsync(_pendingUpdates,true, ct);
        }

        // Clear pending operations
        _pendingCreates.Clear();
        _pendingUpdates.Clear();
    }

    /// <summary>
    /// Clears all caches and pending operations (for new match).
    /// </summary>
    public void ClearPending()
    {
        _pendingCreates.Clear();
        _pendingUpdates.Clear();
    }
}

/// <summary>
/// Represents a pending player creation.
/// </summary>
public class PendingPlayer
{
    public string Name { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
}

