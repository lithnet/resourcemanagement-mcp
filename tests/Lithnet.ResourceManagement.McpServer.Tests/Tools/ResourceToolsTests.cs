using System.Text.Json;
using Lithnet.ResourceManagement.McpServer.Tools;
using ModelContextProtocol;
using Xunit;

namespace Lithnet.ResourceManagement.McpServer.Tests.Tools;

public class ResourceToolsTests
{
    [Fact]
    public void ValidateGetResourceParameters_BothIdAndKeyProvided_Throws()
    {
        Assert.Throws<McpException>(() =>
            ResourceTools.ValidateGetResourceParameters("some-guid", "Person", "AccountName", "admin"));
    }

    [Fact]
    public void ValidateGetResourceParameters_NeitherIdNorKeyProvided_Throws()
    {
        Assert.Throws<McpException>(() =>
            ResourceTools.ValidateGetResourceParameters(null, null, null, null));
    }

    [Fact]
    public void ValidateGetResourceParameters_EmptyStrings_Throws()
    {
        Assert.Throws<McpException>(() =>
            ResourceTools.ValidateGetResourceParameters("", "", "", ""));
    }

    [Fact]
    public void ValidateGetResourceParameters_IdOnly_DoesNotThrow()
    {
        ResourceTools.ValidateGetResourceParameters("some-guid", null, null, null);
    }

    [Fact]
    public void ValidateGetResourceParameters_KeyOnly_DoesNotThrow()
    {
        ResourceTools.ValidateGetResourceParameters(null, "Person", "AccountName", "admin");
    }

    [Fact]
    public void ValidateGetResourceParameters_PartialKey_MissingAttributeValue_Throws()
    {
        Assert.Throws<McpException>(() =>
            ResourceTools.ValidateGetResourceParameters(null, "Person", "AccountName", null));
    }

    [Fact]
    public void ValidateUpdateResourceParameters_NoChangesProvided_Throws()
    {
        Assert.Throws<McpException>(() =>
            ResourceTools.ValidateUpdateResourceParameters(null, null, null));
    }

    [Fact]
    public void ValidateUpdateResourceParameters_EmptyDictionaries_Throws()
    {
        Assert.Throws<McpException>(() =>
            ResourceTools.ValidateUpdateResourceParameters(
                new Dictionary<string, JsonElement>(),
                new Dictionary<string, JsonElement>(),
                new Dictionary<string, JsonElement>()));
    }

    [Fact]
    public void ValidateUpdateResourceParameters_WithAttributes_DoesNotThrow()
    {
        var attributes = new Dictionary<string, JsonElement>
        {
            { "DisplayName", JsonDocument.Parse("\"Test\"").RootElement }
        };

        ResourceTools.ValidateUpdateResourceParameters(attributes, null, null);
    }

    [Fact]
    public void ValidateUpdateResourceParameters_WithAddValues_DoesNotThrow()
    {
        var addValues = new Dictionary<string, JsonElement>
        {
            { "ProxyAddresses", JsonDocument.Parse("\"smtp:test@example.com\"").RootElement }
        };

        ResourceTools.ValidateUpdateResourceParameters(null, addValues, null);
    }

    [Fact]
    public void ValidateUpdateResourceParameters_WithRemoveValues_DoesNotThrow()
    {
        var removeValues = new Dictionary<string, JsonElement>
        {
            { "ProxyAddresses", JsonDocument.Parse("\"smtp:old@example.com\"").RootElement }
        };

        ResourceTools.ValidateUpdateResourceParameters(null, null, removeValues);
    }

    [Fact]
    public void ValidateDeleteResourceParameters_EmptyObjectId_Throws()
    {
        Assert.Throws<McpException>(() =>
            ResourceTools.ValidateDeleteResourceParameters(""));
    }

    [Fact]
    public void ValidateDeleteResourceParameters_NullObjectId_Throws()
    {
        Assert.Throws<McpException>(() =>
            ResourceTools.ValidateDeleteResourceParameters(null));
    }

    [Fact]
    public void ValidateDeleteResourceParameters_ValidObjectId_DoesNotThrow()
    {
        ResourceTools.ValidateDeleteResourceParameters("7fb2b853-24f0-4498-9534-4e10589723c4");
    }

    [Fact]
    public void ConvertJsonElement_String_ReturnsString()
    {
        var element = JsonDocument.Parse("\"hello\"").RootElement;

        object result = ResourceTools.ConvertJsonElement(element);

        Assert.IsType<string>(result);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void ConvertJsonElement_Integer_ReturnsLong()
    {
        var element = JsonDocument.Parse("42").RootElement;

        object result = ResourceTools.ConvertJsonElement(element);

        Assert.IsType<long>(result);
        Assert.Equal(42L, result);
    }

    [Fact]
    public void ConvertJsonElement_Double_ReturnsDouble()
    {
        var element = JsonDocument.Parse("3.14").RootElement;

        object result = ResourceTools.ConvertJsonElement(element);

        Assert.IsType<double>(result);
        Assert.Equal(3.14, result);
    }

    [Fact]
    public void ConvertJsonElement_BooleanTrue_ReturnsTrue()
    {
        var element = JsonDocument.Parse("true").RootElement;

        object result = ResourceTools.ConvertJsonElement(element);

        Assert.IsType<bool>(result);
        Assert.Equal(true, result);
    }

    [Fact]
    public void ConvertJsonElement_BooleanFalse_ReturnsFalse()
    {
        var element = JsonDocument.Parse("false").RootElement;

        object result = ResourceTools.ConvertJsonElement(element);

        Assert.IsType<bool>(result);
        Assert.Equal(false, result);
    }

    [Fact]
    public void ConvertJsonElement_NegativeInteger_ReturnsLong()
    {
        var element = JsonDocument.Parse("-100").RootElement;

        object result = ResourceTools.ConvertJsonElement(element);

        Assert.IsType<long>(result);
        Assert.Equal(-100L, result);
    }

    [Fact]
    public void ConvertJsonElement_Zero_ReturnsLong()
    {
        var element = JsonDocument.Parse("0").RootElement;

        object result = ResourceTools.ConvertJsonElement(element);

        Assert.IsType<long>(result);
        Assert.Equal(0L, result);
    }

    [Fact]
    public void ConvertJsonElement_Object_Throws()
    {
        var element = JsonDocument.Parse("{\"key\":\"value\"}").RootElement;

        Assert.Throws<McpException>(() => ResourceTools.ConvertJsonElement(element));
    }

    [Fact]
    public void ConvertJsonElement_Null_ReturnsNull()
    {
        var element = JsonDocument.Parse("null").RootElement;

        object result = ResourceTools.ConvertJsonElement(element);

        Assert.Null(result);
    }
}
