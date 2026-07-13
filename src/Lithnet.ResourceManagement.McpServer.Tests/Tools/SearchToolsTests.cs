using Lithnet.ResourceManagement.McpServer.Models;
using Lithnet.ResourceManagement.McpServer.Tools;
using ModelContextProtocol;
using Xunit;

namespace Lithnet.ResourceManagement.McpServer.Tests.Tools;

public class SearchToolsTests
{
    [Fact]
    public void BuildXPathFromFilters_SingleEqualsFilter_ProducesCorrectXPath()
    {
        var filters = new[]
        {
            new SearchFilter { Attribute = "Name", Operator = "Equals", Value = "John" }
        };

        string result = SearchTools.BuildXPathFromFilters("Person", filters, "And");

        Assert.Equal("/Person[Name='John']", result);
    }

    [Fact]
    public void BuildXPathFromFilters_MultipleFiltersWithAnd_JoinsWithAnd()
    {
        var filters = new[]
        {
            new SearchFilter { Attribute = "Name", Operator = "Equals", Value = "John" },
            new SearchFilter { Attribute = "Domain", Operator = "Equals", Value = "CORP" }
        };

        string result = SearchTools.BuildXPathFromFilters("Person", filters, "And");

        Assert.Equal("/Person[Name='John' and Domain='CORP']", result);
    }

    [Fact]
    public void BuildXPathFromFilters_MultipleFiltersWithOr_JoinsWithOr()
    {
        var filters = new[]
        {
            new SearchFilter { Attribute = "Name", Operator = "Equals", Value = "John" },
            new SearchFilter { Attribute = "Name", Operator = "Equals", Value = "Jane" }
        };

        string result = SearchTools.BuildXPathFromFilters("Person", filters, "Or");

        Assert.Equal("/Person[Name='John' or Name='Jane']", result);
    }

    [Fact]
    public void BuildXPathFromFilters_IsPresentOperator_ProducesBareAttributeName()
    {
        var filters = new[]
        {
            new SearchFilter { Attribute = "Email", Operator = "IsPresent" }
        };

        string result = SearchTools.BuildXPathFromFilters("Person", filters, "And");

        Assert.Equal("/Person[Email]", result);
    }

    [Fact]
    public void BuildXPathFromFilters_NotPresentOperator_ProducesNotPredicate()
    {
        var filters = new[]
        {
            new SearchFilter { Attribute = "Email", Operator = "NotPresent" }
        };

        string result = SearchTools.BuildXPathFromFilters("Person", filters, "And");

        Assert.Equal("/Person[not(Email)]", result);
    }

    [Fact]
    public void BuildXPathFromFilters_ContainsOperator_ProducesContainsPredicate()
    {
        var filters = new[]
        {
            new SearchFilter { Attribute = "Name", Operator = "Contains", Value = "oh" }
        };

        string result = SearchTools.BuildXPathFromFilters("Person", filters, "And");

        Assert.Equal("/Person[contains(Name,'oh')]", result);
    }

    [Fact]
    public void BuildXPathFromFilters_StartsWithOperator_ProducesStartsWithPredicate()
    {
        var filters = new[]
        {
            new SearchFilter { Attribute = "Name", Operator = "StartsWith", Value = "J" }
        };

        string result = SearchTools.BuildXPathFromFilters("Person", filters, "And");

        Assert.Equal("/Person[starts-with(Name,'J')]", result);
    }

    [Fact]
    public void BuildXPathFromFilters_EndsWithOperator_ProducesEndsWithPredicate()
    {
        var filters = new[]
        {
            new SearchFilter { Attribute = "Name", Operator = "EndsWith", Value = "n" }
        };

        string result = SearchTools.BuildXPathFromFilters("Person", filters, "And");

        Assert.Equal("/Person[ends-with(Name,'n')]", result);
    }

    [Fact]
    public void BuildXPathFromFilters_GreaterThanOperator_ProducesGreaterThanPredicate()
    {
        var filters = new[]
        {
            new SearchFilter { Attribute = "Age", Operator = "GreaterThan", Value = "30" }
        };

        string result = SearchTools.BuildXPathFromFilters("Person", filters, "And");

        Assert.Equal("/Person[Age>'30']", result);
    }

    [Fact]
    public void BuildXPathFromFilters_ValueWithSingleQuote_SwitchesToDoubleQuoteDelimiters()
    {
        var filters = new[]
        {
            new SearchFilter { Attribute = "Name", Operator = "Equals", Value = "O'Brien" }
        };

        string result = SearchTools.BuildXPathFromFilters("Person", filters, "And");

        Assert.Equal("/Person[Name=\"O'Brien\"]", result);
    }

    [Fact]
    public void QuoteXPathValue_ValueWithBothQuoteTypes_Throws()
    {
        Assert.Throws<McpException>(() =>
            SearchTools.QuoteXPathValue("it's a \"test\""));
    }

    [Fact]
    public void BuildXPathFromFilters_InvalidOperator_ThrowsArgumentException()
    {
        var filters = new[]
        {
            new SearchFilter { Attribute = "Name", Operator = "FooBar", Value = "test" }
        };

        Assert.Throws<McpException>(() =>
            SearchTools.BuildXPathFromFilters("Person", filters, "And"));
    }

    [Fact]
    public void BuildXPathFromFilters_InvalidFilterOperator_ThrowsArgumentException()
    {
        var filters = new[]
        {
            new SearchFilter { Attribute = "Name", Operator = "Equals", Value = "John" }
        };

        Assert.Throws<McpException>(() =>
            SearchTools.BuildXPathFromFilters("Person", filters, "Xor"));
    }
}
