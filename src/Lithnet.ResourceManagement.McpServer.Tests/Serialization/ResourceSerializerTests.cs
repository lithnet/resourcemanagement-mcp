using System.Reflection;
using Lithnet.ResourceManagement.Client;
using Lithnet.ResourceManagement.McpServer.Serialization;
using NSubstitute;
using Xunit;

namespace Lithnet.ResourceManagement.McpServer.Tests.Serialization;

public class ResourceSerializerTests
{
    private static AttributeTypeDefinition CreateAttributeTypeDefinition(string name, AttributeType type, bool isMultivalued)
    {
        var ctor = typeof(AttributeTypeDefinition).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(AttributeType), typeof(bool), typeof(bool), typeof(bool) },
            null);

        return (AttributeTypeDefinition)ctor.Invoke(new object[] { name, type, isMultivalued, false, false });
    }

    private static AttributeValue CreateAttributeValue(AttributeTypeDefinition typeDef, object value)
    {
        var ctor = typeof(AttributeValue).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(AttributeTypeDefinition), typeof(object) },
            null);

        return (AttributeValue)ctor.Invoke(new object[] { typeDef, value });
    }

    private static AttributeValue CreateNullAttributeValue(AttributeTypeDefinition typeDef)
    {
        var ctor = typeof(AttributeValue).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(AttributeTypeDefinition) },
            null);

        return (AttributeValue)ctor.Invoke(new object[] { typeDef });
    }

    private static IResourceObject CreateMockResource(params (string name, AttributeValue value)[] attributes)
    {
        var resource = Substitute.For<IResourceObject>();
        var collection = Substitute.For<IAttributeValueCollection>();

        var attributeList = new List<AttributeValue>();

        foreach (var (name, value) in attributes)
        {
            collection[name].Returns(value);
            attributeList.Add(value);
        }

        collection.GetEnumerator().Returns(_ => attributeList.GetEnumerator());
        resource.Attributes.Returns(collection);

        return resource;
    }

    [Fact]
    public void Serialize_StringAttribute_ReturnsStringValue()
    {
        var typeDef = CreateAttributeTypeDefinition("DisplayName", AttributeType.String, false);
        var attrValue = CreateAttributeValue(typeDef, "Test User");
        var resource = CreateMockResource(("DisplayName", attrValue));

        var result = ResourceSerializer.Serialize(resource);

        Assert.True(result.ContainsKey("DisplayName"));
        Assert.Equal("Test User", result["DisplayName"]);
    }

    [Fact]
    public void Serialize_IntegerAttribute_ReturnsLongValue()
    {
        var typeDef = CreateAttributeTypeDefinition("Age", AttributeType.Integer, false);
        var attrValue = CreateAttributeValue(typeDef, 42L);
        var resource = CreateMockResource(("Age", attrValue));

        var result = ResourceSerializer.Serialize(resource);

        Assert.True(result.ContainsKey("Age"));
        Assert.Equal(42L, result["Age"]);
    }

    [Fact]
    public void Serialize_BooleanAttribute_ReturnsBoolValue()
    {
        var typeDef = CreateAttributeTypeDefinition("IsActive", AttributeType.Boolean, false);
        var attrValue = CreateAttributeValue(typeDef, true);
        var resource = CreateMockResource(("IsActive", attrValue));

        var result = ResourceSerializer.Serialize(resource);

        Assert.True(result.ContainsKey("IsActive"));
        Assert.Equal(true, result["IsActive"]);
    }

    [Fact]
    public void Serialize_DateTimeAttribute_ReturnsIso8601String()
    {
        var dateTime = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var typeDef = CreateAttributeTypeDefinition("CreatedTime", AttributeType.DateTime, false);
        var attrValue = CreateAttributeValue(typeDef, dateTime);
        var resource = CreateMockResource(("CreatedTime", attrValue));

        var result = ResourceSerializer.Serialize(resource);

        Assert.True(result.ContainsKey("CreatedTime"));
        string isoString = (string)result["CreatedTime"];
        Assert.Equal(dateTime.ToString("O"), isoString);
    }

    [Fact]
    public void Serialize_ReferenceAttribute_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();
        var uniqueId = new UniqueIdentifier(guid);
        var typeDef = CreateAttributeTypeDefinition("Creator", AttributeType.Reference, false);
        var attrValue = CreateAttributeValue(typeDef, uniqueId);
        var resource = CreateMockResource(("Creator", attrValue));

        var result = ResourceSerializer.Serialize(resource);

        Assert.True(result.ContainsKey("Creator"));
        Assert.Equal(uniqueId.Value, result["Creator"]);
    }

    [Fact]
    public void Serialize_BinaryAttribute_ReturnsBase64String()
    {
        byte[] binaryData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var typeDef = CreateAttributeTypeDefinition("Photo", AttributeType.Binary, false);
        var attrValue = CreateAttributeValue(typeDef, binaryData);
        var resource = CreateMockResource(("Photo", attrValue));

        var result = ResourceSerializer.Serialize(resource);

        Assert.True(result.ContainsKey("Photo"));
        Assert.Equal(Convert.ToBase64String(binaryData), result["Photo"]);
    }

    [Fact]
    public void Serialize_MultivaluedStringAttribute_ReturnsStringList()
    {
        var typeDef = CreateAttributeTypeDefinition("ProxyAddresses", AttributeType.String, true);
        var values = new List<object> { "SMTP:user@example.com", "smtp:alias@example.com" };
        var attrValue = CreateAttributeValue(typeDef, values);
        var resource = CreateMockResource(("ProxyAddresses", attrValue));

        var result = ResourceSerializer.Serialize(resource);

        Assert.True(result.ContainsKey("ProxyAddresses"));
        var list = Assert.IsType<List<string>>(result["ProxyAddresses"]);
        Assert.Equal(2, list.Count);
        Assert.Contains("SMTP:user@example.com", list);
        Assert.Contains("smtp:alias@example.com", list);
    }

    [Fact]
    public void Serialize_MultivaluedReferenceAttribute_ReturnsGuidStringList()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var typeDef = CreateAttributeTypeDefinition("ExplicitMember", AttributeType.Reference, true);
        var values = new List<object> { new UniqueIdentifier(guid1), new UniqueIdentifier(guid2) };
        var attrValue = CreateAttributeValue(typeDef, values);
        var resource = CreateMockResource(("ExplicitMember", attrValue));

        var result = ResourceSerializer.Serialize(resource);

        Assert.True(result.ContainsKey("ExplicitMember"));
        var list = Assert.IsType<List<string>>(result["ExplicitMember"]);
        Assert.Equal(2, list.Count);
        Assert.Contains(new UniqueIdentifier(guid1).Value, list);
        Assert.Contains(new UniqueIdentifier(guid2).Value, list);
    }

    [Fact]
    public void Serialize_NullAttribute_IsOmittedFromOutput()
    {
        var typeDef = CreateAttributeTypeDefinition("Description", AttributeType.String, false);
        var attrValue = CreateNullAttributeValue(typeDef);
        var resource = CreateMockResource(("Description", attrValue));

        var result = ResourceSerializer.Serialize(resource);

        Assert.False(result.ContainsKey("Description"));
    }

    [Fact]
    public void Serialize_WithAttributeNames_ReturnsOnlyRequestedAttributes()
    {
        var typeDef1 = CreateAttributeTypeDefinition("DisplayName", AttributeType.String, false);
        var attrValue1 = CreateAttributeValue(typeDef1, "Test User");

        var typeDef2 = CreateAttributeTypeDefinition("AccountName", AttributeType.String, false);
        var attrValue2 = CreateAttributeValue(typeDef2, "testuser");

        var typeDef3 = CreateAttributeTypeDefinition("Description", AttributeType.String, false);
        var attrValue3 = CreateAttributeValue(typeDef3, "A test user");

        var resource = CreateMockResource(
            ("DisplayName", attrValue1),
            ("AccountName", attrValue2),
            ("Description", attrValue3));

        var result = ResourceSerializer.Serialize(resource, new[] { "DisplayName", "AccountName" });

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("DisplayName"));
        Assert.True(result.ContainsKey("AccountName"));
        Assert.False(result.ContainsKey("Description"));
    }
}
