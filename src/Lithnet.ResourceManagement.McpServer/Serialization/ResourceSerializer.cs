using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.McpServer.Serialization;

public static class ResourceSerializer
{
    public static Dictionary<string, object> Serialize(IResourceObject resource, IEnumerable<string> attributeNames = null)
    {
        var result = new Dictionary<string, object>();

        if (attributeNames != null)
        {
            foreach (string name in attributeNames)
            {
                AttributeValue attr = resource.Attributes[name];
                if (attr == null || attr.IsNull)
                {
                    continue;
                }

                object serialized = SerializeAttributeValue(attr);
                if (serialized != null)
                {
                    result[name] = serialized;
                }
            }
        }
        else
        {
            foreach (AttributeValue attr in resource.Attributes)
            {
                if (attr.IsNull)
                {
                    continue;
                }

                object serialized = SerializeAttributeValue(attr);
                if (serialized != null)
                {
                    result[attr.AttributeName] = serialized;
                }
            }
        }

        return result;
    }

    private static object SerializeAttributeValue(AttributeValue attr)
    {
        if (attr.Attribute.IsMultivalued)
        {
            return SerializeMultivalued(attr);
        }

        return SerializeSingleValued(attr);
    }

    private static object SerializeSingleValued(AttributeValue attr)
    {
        switch (attr.Attribute.Type)
        {
            case AttributeType.String:
                return attr.StringValue;

            case AttributeType.Integer:
                return attr.IntegerValue;

            case AttributeType.Boolean:
                return attr.BooleanValue;

            case AttributeType.DateTime:
                return attr.DateTimeValue.ToString("O");

            case AttributeType.Reference:
                return attr.ReferenceValue.Value;

            case AttributeType.Binary:
                return Convert.ToBase64String(attr.BinaryValue);

            default:
                return attr.Value?.ToString();
        }
    }

    private static object SerializeMultivalued(AttributeValue attr)
    {
        switch (attr.Attribute.Type)
        {
            case AttributeType.String:
                return attr.StringValues.ToList();

            case AttributeType.Integer:
                return attr.IntegerValues.ToList();

            case AttributeType.Reference:
                return attr.ReferenceValues.Select(r => r.Value).ToList();

            case AttributeType.DateTime:
                return attr.DateTimeValues.Select(d => d.ToString("O")).ToList();

            case AttributeType.Binary:
                return attr.BinaryValues.Select(b => Convert.ToBase64String(b)).ToList();

            default:
                return attr.ValuesAsString.ToList();
        }
    }
}
