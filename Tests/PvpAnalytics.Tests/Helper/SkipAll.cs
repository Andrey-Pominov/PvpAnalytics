using Xunit;

namespace PvpAnalytics.Tests.Helper;

/// <summary>
/// Attribute for selectively skipping expensive/integration tests.
///
/// By default, tests marked with [SkipAll] are skipped to keep the suite fast.
/// To run them, set the environment variable RUN_INTEGRATION_TESTS=true
/// (or 1 / yes) before running the test suite.
/// </summary>
public class SkipAll : FactAttribute
{
    private const string EnvVarName = "RUN_INTEGRATION_TESTS";

    public SkipAll()
    {
        var value = Environment.GetEnvironmentVariable(EnvVarName);
        if (value is null)
        {
            Skip = $"Skipped integration test. Set {EnvVarName}=true to enable.";
            return;
        }

        if (value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            // Do not set Skip -> test runs
            return;
        }

        Skip = $"Skipped integration test. Set {EnvVarName}=true to enable.";
    }
}