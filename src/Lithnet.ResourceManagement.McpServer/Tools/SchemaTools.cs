using System.ComponentModel;
using System.Text.Json;
using Lithnet.ResourceManagement.Client;
using ModelContextProtocol.Server;

namespace Lithnet.ResourceManagement.McpServer.Tools;

[McpServerToolType]
public static class SchemaTools
{
    private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true };

    [McpServerTool(Name = "refresh_schema")]
    [Description("Refresh the cached MIM schema. Call this after creating or modifying AttributeTypeDescription, BindingDescription, or ObjectTypeDescription resources so that subsequent get_schema_attributes and get_schema_object_types calls return up-to-date results.")]
    public static string RefreshSchema(MimClientFactory clientFactory)
    {
        var client = clientFactory.GetClient();
        client.RefreshSchema();
        return JsonSerializer.Serialize(new { status = "refreshed" }, jsonOptions);
    }

    [McpServerTool(Name = "get_schema_object_types")]
    [Description("List all object types defined in the MIM service. Returns name, display name, and description for each type.")]
    public static string GetSchemaObjectTypes(MimClientFactory clientFactory)
    {
        var client = clientFactory.GetClient();
        var results = client.GetResources(
            "/ObjectTypeDescription",
            new[] { "Name", "DisplayName", "Description" });

        var objectTypes = new List<Dictionary<string, string>>();

        foreach (var resource in results)
        {
            var entry = new Dictionary<string, string>();
            entry["name"] = resource.Attributes["Name"].IsNull ? null : resource.Attributes["Name"].StringValue;
            entry["displayName"] = resource.Attributes["DisplayName"].IsNull ? null : resource.Attributes["DisplayName"].StringValue;
            entry["description"] = resource.Attributes["Description"].IsNull ? null : resource.Attributes["Description"].StringValue;
            objectTypes.Add(entry);
        }

        objectTypes.Sort((a, b) => string.Compare(a["name"], b["name"], StringComparison.OrdinalIgnoreCase));

        return JsonSerializer.Serialize(objectTypes, jsonOptions);
    }

    [McpServerTool(Name = "get_schema_attributes")]
    [Description("Get the attributes bound to an object type, including name, display name, data type, whether it is multivalued, required, read-only, its validation regex, and description.")]
    public static string GetSchemaAttributes(
        MimClientFactory clientFactory,
        [Description("The object type name, e.g. 'Person', 'Group'")] string objectType)
    {
        var client = clientFactory.GetClient();
        var objectTypeDef = client.GetObjectType(objectType);

        var attributes = new List<Dictionary<string, object>>();

        foreach (var attr in objectTypeDef.Attributes.OrderBy(a => a.SystemName))
        {
            var entry = new Dictionary<string, object>();
            entry["name"] = attr.SystemName;
            entry["displayName"] = attr.DisplayName;
            entry["dataType"] = attr.Type.ToString();
            entry["multivalued"] = attr.IsMultivalued;
            entry["required"] = attr.IsRequired;
            entry["readOnly"] = attr.IsReadOnly;

            if (attr.Regex != null)
            {
                entry["regex"] = attr.Regex;
            }

            if (attr.Description != null)
            {
                entry["description"] = attr.Description;
            }

            attributes.Add(entry);
        }

        return JsonSerializer.Serialize(attributes, jsonOptions);
    }

    [McpServerTool(Name = "get_rcdc")]
    [Description("Get the RCDC (Resource Control Display Configuration) XML for an object type. Returns the configuration XML used to render create/edit/view forms in the MIM Portal.")]
    public static string GetRcdc(
        MimClientFactory clientFactory,
        [Description("The object type name, e.g. 'Person', 'Group'")] string objectType,
        [Description("Optional mode filter: 'create', 'edit', or 'view'. If omitted, returns all RCDCs for the type.")] string mode = null)
    {
        var client = clientFactory.GetClient();
        client.GetObjectType(objectType);

        string xpath = $"/ObjectVisualizationConfiguration[TargetObjectType=/ObjectTypeDescription[Name='{objectType}']]";
        var results = client.GetResources(
            xpath,
            new[] { "DisplayName", "ConfigurationData", "StringResources", "AppliesToCreate", "AppliesToEdit", "AppliesToView" });

        var rcdcs = new List<Dictionary<string, string>>();

        foreach (var resource in results)
        {
            string rcdcMode = DetermineRcdcMode(resource);

            if (mode != null && !string.Equals(rcdcMode, mode, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var entry = new Dictionary<string, string>();
            entry["displayName"] = resource.Attributes["DisplayName"].IsNull ? null : resource.Attributes["DisplayName"].StringValue;
            entry["mode"] = rcdcMode;
            entry["configurationData"] = resource.Attributes["ConfigurationData"].IsNull ? null : resource.Attributes["ConfigurationData"].StringValue;
            entry["stringResources"] = resource.Attributes["StringResources"].IsNull ? null : resource.Attributes["StringResources"].StringValue;
            rcdcs.Add(entry);
        }

        return JsonSerializer.Serialize(rcdcs, jsonOptions);
    }

    internal static string DetermineRcdcMode(IResourceObject resource)
    {
        if (!resource.Attributes["AppliesToCreate"].IsNull && resource.Attributes["AppliesToCreate"].BooleanValue)
        {
            return "create";
        }

        if (!resource.Attributes["AppliesToEdit"].IsNull && resource.Attributes["AppliesToEdit"].BooleanValue)
        {
            return "edit";
        }

        return "view";
    }
}
