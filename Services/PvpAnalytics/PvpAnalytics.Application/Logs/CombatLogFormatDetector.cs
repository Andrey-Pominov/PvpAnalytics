using System.Text;

namespace PvpAnalytics.Application.Logs;

/// <summary>
/// Detects the format of a combat log file by examining the beginning of the stream.
/// </summary>
public static class CombatLogFormatDetector
{
    private const int PeekSize = 100; // Bytes to peek at for detection

    /// <summary>
    /// Detects the format of a combat log stream without consuming it.
    /// </summary>
    /// <param name="stream">The stream to detect format from. Must support seeking or peeking.</param>
    /// <returns>The detected format type.</returns>
    public static CombatLogFormat DetectFormat(Stream stream)
    {
        if (!stream.CanSeek)
        {
            // If stream doesn't support seeking, we need to peek using a buffer
            return DetectFormatFromPeek(stream);
        }

        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            var buffer = new byte[PeekSize];
            var bytesRead = stream.Read(buffer, 0, PeekSize);
            
            if (bytesRead == 0)
            {
                return CombatLogFormat.Traditional; // Default to traditional for empty streams
            }

            var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
            // Check for Lua table format
            if (text.TrimStart().StartsWith("PvPAnalyticsDB", StringComparison.OrdinalIgnoreCase))
            {
                return CombatLogFormat.LuaTable;
            }

            // Default to traditional format
            return CombatLogFormat.Traditional;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    private static CombatLogFormat DetectFormatFromPeek(Stream stream)
    {
        var originalPosition = stream.Position;
        var buffer = new byte[PeekSize];
        var bytesRead = stream.Read(buffer, 0, PeekSize);
        
        try
        {
            if (bytesRead == 0)
            {
                return CombatLogFormat.Traditional;
            }

            var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
            if (text.TrimStart().StartsWith("PvPAnalyticsDB", StringComparison.OrdinalIgnoreCase))
            {
                return CombatLogFormat.LuaTable;
            }

            return CombatLogFormat.Traditional;
        }
        finally
        {
            // Try to restore position if possible
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }
}

