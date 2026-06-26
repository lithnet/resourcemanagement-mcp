namespace Lithnet.ResourceManagement.McpServer;

public static class ServerInstructions
{
    public const string Text = """
        MIM MCP Server: operational guide for Microsoft Identity Manager (MIM) Service.

        ## Resource model
        Everything in MIM is a resource with an ObjectID (GUID), ObjectType, and typed attributes.
        Attribute data types: String, Text, Integer, Boolean, DateTime, Reference, Binary.
        Reference attributes store GUIDs pointing to other resources. Resolve them with get_resource.
        You cannot filter on Text or Binary attributes in XPath queries.

        ## XPath syntax
        Queries use a subset of XPath 2.0: /ObjectType[predicate]
        XPath is CASE-SENSITIVE. Attribute names must match the SystemName exactly.

        Operators: =, !=, <, >, <=, >=
        Boolean: and, or
        Functions: contains(), starts-with(), ends-with(), not()

        IMPORTANT: contains() matches after WORD-BREAKING characters only, not arbitrary substrings.
        contains(DisplayName, 'mit') does NOT match "Smith" because 'mit' is mid-word.

        The not() function's argument must be an equality expression using = only. No !=, no nested functions.

        ### The != operator
        - If the attribute is NULL and the right-hand side is a literal value, != returns FALSE (not true).
        - If the attribute is NULL and the right-hand side is a location path (e.g. /Person), != returns TRUE.
        - != is NOT supported on multi-valued attributes.

        ### Multi-valued attributes
        When using = with a multi-valued attribute on the left, the expression is true if ANY value matches:
        /Group[ComputedMember = '11111111-1111-1111-1111-111111111111'] returns groups containing that member.

        ### DateTime values
        Use ISO 8601 format without timezone: /Person[CreatedTime >= '2024-01-15T10:30:00']
        Date arithmetic functions: current-dateTime(), add-dayTimeDuration-to-dateTime(), subtract-dayTimeDuration-from-dateTime()
        Example (groups expiring in 7 days): /Group[ExpirationTime <= op:add-dayTimeDuration-to-dateTime(fn:current-dateTime(), xs:dayTimeDuration('P7D'))]

        ### Union queries
        Use | to combine queries: /Person[Domain = 'CORP'] | /Person[Domain = 'DMZ']

        ### Location path chains
        Follow reference attributes through resources:
        /Person[AccountName = 'jsmith']/Manager returns the manager's Person resource.
        Each step must be a reference attribute. /Person/DisplayName is INVALID (DisplayName is a String, not a Reference).

        ### Reference attributes in XPath
        Two ways to compare reference attributes:

        1. Direct GUID: /Person[Manager = '7fb2b853-24f0-4498-9534-4e10589723c4']
           Use the bare GUID string (no prefix). This works for any reference attribute.

        2. Dereferencing: /Person[Manager = /Person[AccountName = 'jsmith']]
           Resolves the right-hand side to find matching ObjectIDs. Useful when you don't have the GUID but know an attribute value of the target.

        Both are valid. Use whichever fits.

        WARNING: Do NOT use the urn:uuid: prefix in XPath. /Person[Manager = 'urn:uuid:7fb2...'] will FAIL. Always use bare GUIDs.

        ### String quoting
        Use single quotes by default: /Person[AccountName = 'jsmith']
        If the value contains a single quote, switch to double quotes: /Person[LastName = "O'Brien"]
        Do NOT use SQL-style doubled single quotes. That is not valid in MIM XPath.

        ### Checking attribute presence
        Reference attributes: /Person[Manager = /Person] (has manager), /Person[Manager != /Person] (no manager)
        Multi-valued reference: /Group[not(Owner = /Person)] (no owner)
        Non-reference: /Person[AccountName != '&Invalid&'] (has value). This works because != returns false for null attributes compared to literals.

        ## Schema
        ObjectTypeDescription defines resource types. AttributeTypeDescription defines attributes.
        BindingDescription links an attribute to a type (with Required flag).
        After creating schema resources, call refresh_schema so subsequent queries see the new attributes.

        ## Schema creation
        To add an attribute to an object type:
        1. Create an AttributeTypeDescription resource (Name, DisplayName, DataType, Multivalued)
        2. Create a BindingDescription resource (BoundAttributeType = the attribute's ObjectID, BoundObjectType = the target ObjectTypeDescription's ObjectID, Required = true/false)
        3. Call refresh_schema so subsequent queries see the new attributes
        After creating a new ObjectTypeDescription, run iisreset on the MIM server to refresh the portal's schema cache.

        ## Sets
        Sets define resource collections used in policy. They have:
        - Filter: XPath-based dynamic membership
        - ExplicitMember: manually added references
        - ComputedMember: read-only union of both

        The Filter attribute value is NOT raw XPath. It must be wrapped in an XML element:
        <Filter xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" Dialect="http://schemas.microsoft.com/2006/11/XPathFilterDialect" xmlns="http://schemas.xmlsoap.org/ws/2004/09/enumeration">/Person[EmployeeType = 'Full Time Employee']</Filter>

        Set filters are more restricted than ad-hoc queries: contains() and location-path right-hand terms are not allowed.

        ## Management Policy Rules (MPRs)
        MPRs control access and trigger workflows.
        Request MPRs: evaluate on CRUD operations. PrincipalSet = who, ResourceCurrentSet/ResourceFinalSet = what, ActionType = Create/Read/Modify/Add/Remove/Delete, ActionParameter = which attributes (* for all).
        Set Transition MPRs: trigger on TransitionIn/TransitionOut of a set. Cannot grant rights.

        ## Tool usage
        - search_resources: always specify the attributes parameter. There is no return-everything mode.
        - search_resources maxResults defaults to 50. Set it higher if you need more.
        - get_resource: returns all non-null attributes by default, or specify which ones you want.
        - update_resource: use 'attributes' to set/replace, 'addValues' to add to multivalued, 'removeValues' to remove from multivalued.
        - refresh_schema: call after any schema changes (new AttributeTypeDescription, BindingDescription, ObjectTypeDescription).
        """;
}
