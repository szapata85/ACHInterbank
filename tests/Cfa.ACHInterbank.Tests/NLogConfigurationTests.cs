using System.Xml.Linq;

namespace Cfa.ACHInterbank.Tests;

public sealed class NLogConfigurationTests
{
    [Fact]
    public void Configuration_HasOneAsynchronousFileTarget_WithBoundedArchiveRetention()
    {
        var projectRoot = new DirectoryInfo(AppContext.BaseDirectory);
        while (projectRoot is not null && !File.Exists(Path.Combine(projectRoot.FullName, "ACHInterbank.sln")))
        {
            projectRoot = projectRoot.Parent;
        }

        Assert.NotNull(projectRoot);
        var path = Path.Combine(projectRoot.FullName, "src", "Cfa.ACHInterbank.Api", "NLog.config");
        var xml = XDocument.Load(path);
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        var targets = xml.Descendants().Where(node => node.Name.LocalName == "target").ToArray();
        var wrapper = Assert.Single(targets.Where(node => (string?)node.Attribute(xsi + "type") == "AsyncWrapper"));
        Assert.Equal("Block", (string?)wrapper.Attribute("overflowAction"));
        var file = Assert.Single(targets.Where(node => (string?)node.Attribute(xsi + "type") == "File"));
        Assert.Equal("${basedir}/logs/ACHInterbank.log", (string?)file.Attribute("fileName"));
        Assert.Equal("60", (string?)file.Attribute("maxArchiveDays"));
        Assert.Equal("30", (string?)file.Attribute("maxArchiveFiles"));
        Assert.Equal("10485760", (string?)file.Attribute("archiveAboveSize"));
        Assert.DoesNotContain("crash.log", xml.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("trace.log", xml.ToString(), StringComparison.OrdinalIgnoreCase);

        var rules = xml.Descendants().Where(node => node.Name.LocalName == "logger").ToArray();
        Assert.Contains(rules, rule => (string?)rule.Attribute("name") == "*"
            && (string?)rule.Attribute("writeTo") == "LogData");
    }
}
