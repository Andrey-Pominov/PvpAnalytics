using Xunit;

namespace PvpAnalytics.Tests.Helper;

public class SkipAll : FactAttribute
{
    public SkipAll() => Skip =  "SkipAll";
}