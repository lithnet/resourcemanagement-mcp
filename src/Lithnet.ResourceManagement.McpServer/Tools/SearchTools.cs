using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Lithnet.ResourceManagement.Client;
using Lithnet.ResourceManagement.McpServer.Models;
using Lithnet.ResourceManagement.McpServer.Serialization;
using ModelContextProtocol.Server;

namespace Lithnet.ResourceManagement.McpServer.Tools;

[McpServerToolType]
public static class SearchTools
{
    private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true };

    [McpServerTool(Name = "search_resources")]
    [Description("Search for resources using a raw XPath filter or structured filters. Provide either 'xpath' for raw XPath mode, or 'objectType' and 'filters' for structured mode — not both.")]
    public static string SearchResources(
        MimClientFactory clientFactory,
        [Description("Attribute names to return in the results")] string[] attributes,
        [Description("Raw XPath filter expression, e.g. /Person[Domain='CORP']. Use this OR objectType+filters, not both.")] string xpath = null,
        [Description("The object type to search for, e.g. 'Person', 'Group'. Required for structured mode.")] string objectType = null,
        [Description("Array of filter conditions for structured mode. Each filter has Attribute, Operator, and optionally Value.")] SearchFilter[] filters = null,
        [Description("How to combine multiple filters: 'And' or 'Or'. Default is 'And'.")] string filterOperator = "And",
        [Description("Maximum number of results to return. Default is 50.")] int maxResults = 50,
        [Description("Attribute name to sort results by.")] string sortBy = null,
        [Description("Sort in descending order. Default is false (ascending).")] bool sortDescending = false)
    {
        if (attributes == null || attributes.Length == 0)
        {
            throw new ArgumentException("At least one attribute name must be specified.", nameof(attributes));
        }

        bool hasXPath = !string.IsNullOrWhiteSpace(xpath);
        bool hasStructured = !string.IsNullOrWhiteSpace(objectType) || filters != null;

        if (hasXPath && hasStructured)
        {
            throw new ArgumentException("Provide either 'xpath' for raw XPath mode, or 'objectType' and 'filters' for structured mode — not both.");
        }

        if (!hasXPath && !hasStructured)
        {
            throw new ArgumentException("Either 'xpath' must be provided for raw XPath mode, or 'objectType' and 'filters' must be provided for structured mode.");
        }

        string filter;

        if (hasXPath)
        {
            filter = xpath;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(objectType))
            {
                throw new ArgumentException("'objectType' is required when using structured filter mode.", nameof(objectType));
            }

            if (filters == null || filters.Length == 0)
            {
                throw new ArgumentException("At least one filter must be provided when using structured filter mode.", nameof(filters));
            }

            filter = BuildXPathFromFilters(objectType, filters, filterOperator);
        }

        var client = clientFactory.GetClient();

        List<SortingAttribute> sortAttributes = null;

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            sortAttributes = new List<SortingAttribute>
            {
                new SortingAttribute(sortBy, !sortDescending)
            };
        }

        var results = client.GetResources(filter, 200, attributes, sortAttributes);

        var serialized = new List<Dictionary<string, object>>();
        int count = 0;

        foreach (var resource in results)
        {
            if (count >= maxResults)
            {
                break;
            }

            serialized.Add(ResourceSerializer.Serialize(resource, attributes));
            count++;
        }

        return JsonSerializer.Serialize(serialized, jsonOptions);
    }

    internal static string BuildXPathFromFilters(string objectType, SearchFilter[] filters, string filterOperator)
    {
        if (!Enum.TryParse<GroupOperator>(filterOperator, ignoreCase: true, out var groupOp))
        {
            throw new ArgumentException(
                $"Invalid filter operator '{filterOperator}'. Valid values are: {string.Join(", ", Enum.GetNames(typeof(GroupOperator)))}.",
                nameof(filterOperator));
        }

        string joinOperator = groupOp == GroupOperator.And ? " and " : " or ";

        var predicates = new List<string>();

        foreach (var filter in filters)
        {
            string predicate = BuildPredicate(filter);
            predicates.Add(predicate);
        }

        string combined = string.Join(joinOperator, predicates);

        return $"/{objectType}[{combined}]";
    }

    private static string BuildPredicate(SearchFilter filter)
    {
        string quotedValue = filter.Value != null ? QuoteXPathValue(filter.Value) : "''";

        switch (filter.Operator)
        {
            case "IsPresent":
                return filter.Attribute;

            case "NotPresent":
                return $"not({filter.Attribute})";

            case "Equals":
                return $"{filter.Attribute}={quotedValue}";

            case "NotEquals":
                return $"{filter.Attribute}!={quotedValue}";

            case "Contains":
                return $"contains({filter.Attribute},{quotedValue})";

            case "StartsWith":
                return $"starts-with({filter.Attribute},{quotedValue})";

            case "EndsWith":
                return $"ends-with({filter.Attribute},{quotedValue})";

            case "GreaterThan":
                return $"{filter.Attribute}>{quotedValue}";

            case "LessThan":
                return $"{filter.Attribute}<{quotedValue}";

            case "GreaterThanOrEqual":
                return $"{filter.Attribute}>={quotedValue}";

            case "LessThanOrEqual":
                return $"{filter.Attribute}<={quotedValue}";

            default:
                throw new ArgumentException($"Unknown filter operator '{filter.Operator}'. Valid operators are: Equals, NotEquals, Contains, StartsWith, EndsWith, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual, IsPresent, NotPresent.");
        }
    }

    internal static string QuoteXPathValue(string value)
    {
        if (value.Contains("'"))
        {
            if (value.Contains("\""))
            {
                throw new ArgumentException($"Cannot quote a value that contains both single and double quotes: {value}");
            }

            return $"\"{value}\"";
        }

        return $"'{value}'";
    }
}
