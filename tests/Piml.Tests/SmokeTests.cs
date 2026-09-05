using Xunit;

namespace Piml.Tests;

public class SmokeTests
{
    [Fact]
    public void Library_reports_spec_version()
    {
        Assert.Equal("1.2.0", Piml.SpecVersion);
    }
}
