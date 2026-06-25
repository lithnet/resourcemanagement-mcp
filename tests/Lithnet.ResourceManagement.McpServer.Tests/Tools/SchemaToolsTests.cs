using System.Reflection;
using Lithnet.ResourceManagement.Client;
using Lithnet.ResourceManagement.McpServer.Tools;
using NSubstitute;
using Xunit;

namespace Lithnet.ResourceManagement.McpServer.Tests.Tools;

public class SchemaToolsTests
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

    private static IResourceObject CreateMockResourceWithAttributes(
        bool appliesToCreate, bool appliesToEdit, bool appliesToView)
    {
        var resource = Substitute.For<IResourceObject>();
        var collection = Substitute.For<IAttributeValueCollection>();

        var boolTypeDef = CreateAttributeTypeDefinition("BoolAttr", AttributeType.Boolean, false);

        AttributeValue createValue = appliesToCreate
            ? CreateAttributeValue(boolTypeDef, true)
            : CreateNullAttributeValue(boolTypeDef);

        AttributeValue editValue = appliesToEdit
            ? CreateAttributeValue(boolTypeDef, true)
            : CreateNullAttributeValue(boolTypeDef);

        AttributeValue viewValue = appliesToView
            ? CreateAttributeValue(boolTypeDef, true)
            : CreateNullAttributeValue(boolTypeDef);

        collection["AppliesToCreate"].Returns(createValue);
        collection["AppliesToEdit"].Returns(editValue);
        collection["AppliesToView"].Returns(viewValue);

        resource.Attributes.Returns(collection);

        return resource;
    }

    [Fact]
    public void DetermineRcdcMode_AppliesToCreateTrue_ReturnsCreate()
    {
        var resource = CreateMockResourceWithAttributes(
            appliesToCreate: true, appliesToEdit: false, appliesToView: false);

        string mode = SchemaTools.DetermineRcdcMode(resource);

        Assert.Equal("create", mode);
    }

    [Fact]
    public void DetermineRcdcMode_AppliesToEditTrue_ReturnsEdit()
    {
        var resource = CreateMockResourceWithAttributes(
            appliesToCreate: false, appliesToEdit: true, appliesToView: false);

        string mode = SchemaTools.DetermineRcdcMode(resource);

        Assert.Equal("edit", mode);
    }

    [Fact]
    public void DetermineRcdcMode_AppliesToViewTrue_ReturnsView()
    {
        var resource = CreateMockResourceWithAttributes(
            appliesToCreate: false, appliesToEdit: false, appliesToView: true);

        string mode = SchemaTools.DetermineRcdcMode(resource);

        Assert.Equal("view", mode);
    }

    [Fact]
    public void DetermineRcdcMode_AllNull_ReturnsView()
    {
        var resource = CreateMockResourceWithAttributes(
            appliesToCreate: false, appliesToEdit: false, appliesToView: false);

        string mode = SchemaTools.DetermineRcdcMode(resource);

        Assert.Equal("view", mode);
    }

    [Fact]
    public void DetermineRcdcMode_CreateAndEditBothTrue_ReturnsCreate()
    {
        var resource = CreateMockResourceWithAttributes(
            appliesToCreate: true, appliesToEdit: true, appliesToView: false);

        string mode = SchemaTools.DetermineRcdcMode(resource);

        Assert.Equal("create", mode);
    }

    [Fact]
    public void DetermineRcdcMode_EditAndViewBothTrue_ReturnsEdit()
    {
        var resource = CreateMockResourceWithAttributes(
            appliesToCreate: false, appliesToEdit: true, appliesToView: true);

        string mode = SchemaTools.DetermineRcdcMode(resource);

        Assert.Equal("edit", mode);
    }
}
