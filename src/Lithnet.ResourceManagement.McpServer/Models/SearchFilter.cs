using System.ComponentModel;

namespace Lithnet.ResourceManagement.McpServer.Models;

public class SearchFilter
{
    [Description("The attribute name to filter on")]
    public string Attribute { get; set; }

    [Description("Comparison operator: Equals, NotEquals, Contains, StartsWith, EndsWith, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual, IsPresent, NotPresent")]
    public string Operator { get; set; }

    [Description("The value to compare against. Omit for IsPresent/NotPresent operators.")]
    public string Value { get; set; }
}
