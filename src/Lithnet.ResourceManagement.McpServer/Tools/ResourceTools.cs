using System.ComponentModel;
using System.Text.Json;
using Lithnet.ResourceManagement.Client;
using Lithnet.ResourceManagement.McpServer.Serialization;
using ModelContextProtocol.Server;

namespace Lithnet.ResourceManagement.McpServer.Tools;

[McpServerToolType]
public static class ResourceTools
{
    private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true };

    [McpServerTool(Name = "get_resource")]
    [Description("Get a single resource by ObjectID or by a unique key (object type + attribute name + value). Reference attributes return as GUID strings — call this tool again with that GUID to resolve them.")]
    public static string GetResource(
        MimClientFactory clientFactory,
        [Description("The ObjectID (GUID) of the resource. Mutually exclusive with objectType+attributeName+attributeValue.")] string objectId = null,
        [Description("Object type for key-based lookup, e.g. 'Person'. Use with attributeName and attributeValue.")] string objectType = null,
        [Description("Anchor attribute name for key-based lookup, e.g. 'AccountName'.")] string attributeName = null,
        [Description("Anchor attribute value for key-based lookup.")] string attributeValue = null,
        [Description("Attribute names to return. If omitted, returns all non-null attributes.")] string[] attributes = null)
    {
        ValidateGetResourceParameters(objectId, objectType, attributeName, attributeValue);

        var client = clientFactory.GetClient();
        IResourceObject resource;

        bool hasId = !string.IsNullOrEmpty(objectId);

        if (hasId)
        {
            if (attributes != null)
            {
                resource = client.GetResource(objectId, attributes);
            }
            else
            {
                resource = client.GetResource(objectId);
            }
        }
        else
        {
            if (attributes != null)
            {
                resource = client.GetResourceByKey(objectType, attributeName, attributeValue, attributes, null);
            }
            else
            {
                resource = client.GetResourceByKey(objectType, attributeName, attributeValue);
            }
        }

        var serialized = ResourceSerializer.Serialize(resource, attributes);
        return JsonSerializer.Serialize(serialized, jsonOptions);
    }

    [McpServerTool(Name = "create_resource")]
    [Description("Create a new resource in the MIM service. Returns the created resource's ObjectID and all non-null attributes.")]
    public static string CreateResource(
        MimClientFactory clientFactory,
        [Description("The object type to create, e.g. 'Person', 'Group'.")] string objectType,
        [Description("Key/value pairs of attributes to set. Values can be strings, numbers, booleans, or arrays for multivalued attributes. Reference attributes accept GUID strings.")] Dictionary<string, JsonElement> attributes)
    {
        if (attributes == null || attributes.Count == 0)
        {
            throw new ArgumentException("The 'attributes' parameter is required and must contain at least one attribute.");
        }

        var client = clientFactory.GetClient();
        var resource = client.CreateResource(objectType);

        foreach (var kvp in attributes)
        {
            SetAttributeFromJson(resource, kvp.Key, kvp.Value);
        }

        client.SaveResource(resource);

        var serialized = ResourceSerializer.Serialize(resource);
        return JsonSerializer.Serialize(serialized, jsonOptions);
    }

    [McpServerTool(Name = "update_resource")]
    [Description("Modify attributes on an existing resource. Use 'attributes' to replace values, 'addValues' to add to multivalued attributes, and 'removeValues' to remove from multivalued attributes.")]
    public static string UpdateResource(
        MimClientFactory clientFactory,
        [Description("The ObjectID (GUID) of the resource to update.")] string objectId,
        [Description("Key/value pairs to set (replaces existing values).")] Dictionary<string, JsonElement> attributes = null,
        [Description("Key/value pairs to add to multivalued attributes.")] Dictionary<string, JsonElement> addValues = null,
        [Description("Key/value pairs to remove from multivalued attributes.")] Dictionary<string, JsonElement> removeValues = null)
    {
        ValidateUpdateResourceParameters(attributes, addValues, removeValues);

        var client = clientFactory.GetClient();
        var resource = client.GetResource(objectId);

        if (attributes != null)
        {
            foreach (var kvp in attributes)
            {
                SetAttributeFromJson(resource, kvp.Key, kvp.Value);
            }
        }

        if (addValues != null)
        {
            foreach (var kvp in addValues)
            {
                AddAttributeValueFromJson(resource, kvp.Key, kvp.Value);
            }
        }

        if (removeValues != null)
        {
            foreach (var kvp in removeValues)
            {
                RemoveAttributeValueFromJson(resource, kvp.Key, kvp.Value);
            }
        }

        client.SaveResource(resource);

        return JsonSerializer.Serialize(new { objectId, status = "updated" }, jsonOptions);
    }

    [McpServerTool(Name = "delete_resource")]
    [Description("Delete a resource from the MIM service by its ObjectID.")]
    public static string DeleteResource(
        MimClientFactory clientFactory,
        [Description("The ObjectID (GUID) of the resource to delete.")] string objectId)
    {
        ValidateDeleteResourceParameters(objectId);

        var client = clientFactory.GetClient();
        client.DeleteResource(objectId);

        return JsonSerializer.Serialize(new { objectId, status = "deleted" }, jsonOptions);
    }

    internal static void ValidateGetResourceParameters(string objectId, string objectType, string attributeName, string attributeValue)
    {
        bool hasId = !string.IsNullOrEmpty(objectId);
        bool hasKey = !string.IsNullOrEmpty(objectType) && !string.IsNullOrEmpty(attributeName) && !string.IsNullOrEmpty(attributeValue);

        if (hasId && hasKey)
        {
            throw new ArgumentException("Provide either 'objectId' or 'objectType'+'attributeName'+'attributeValue', not both.");
        }

        if (!hasId && !hasKey)
        {
            throw new ArgumentException("Provide either 'objectId' for ID-based lookup, or 'objectType'+'attributeName'+'attributeValue' for key-based lookup.");
        }
    }

    internal static void ValidateUpdateResourceParameters(Dictionary<string, JsonElement> attributes, Dictionary<string, JsonElement> addValues, Dictionary<string, JsonElement> removeValues)
    {
        if ((attributes == null || attributes.Count == 0) &&
            (addValues == null || addValues.Count == 0) &&
            (removeValues == null || removeValues.Count == 0))
        {
            throw new ArgumentException("At least one of 'attributes', 'addValues', or 'removeValues' must be provided.");
        }
    }

    internal static void ValidateDeleteResourceParameters(string objectId)
    {
        if (string.IsNullOrEmpty(objectId))
        {
            throw new ArgumentException("The 'objectId' parameter is required.");
        }
    }

    private static void SetAttributeFromJson(IResourceObject resource, string attributeName, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            resource.Attributes[attributeName].RemoveValues();
            foreach (var item in value.EnumerateArray())
            {
                resource.Attributes[attributeName].AddValue(ConvertJsonElement(item));
            }
        }
        else if (value.ValueKind == JsonValueKind.Null)
        {
            resource.Attributes[attributeName].RemoveValues();
        }
        else
        {
            resource.Attributes[attributeName].SetValue(ConvertJsonElement(value));
        }
    }

    private static void AddAttributeValueFromJson(IResourceObject resource, string attributeName, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                resource.Attributes[attributeName].AddValue(ConvertJsonElement(item));
            }
        }
        else
        {
            resource.Attributes[attributeName].AddValue(ConvertJsonElement(value));
        }
    }

    private static void RemoveAttributeValueFromJson(IResourceObject resource, string attributeName, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                resource.Attributes[attributeName].RemoveValue(ConvertJsonElement(item));
            }
        }
        else
        {
            resource.Attributes[attributeName].RemoveValue(ConvertJsonElement(value));
        }
    }

    internal static object ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out long longValue))
                {
                    return longValue;
                }
                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.Object:
            case JsonValueKind.Array:
                throw new ArgumentException($"Nested objects and arrays are not supported as attribute values. Got: {element.GetRawText()}");
            default:
                return element.GetRawText();
        }
    }
}
