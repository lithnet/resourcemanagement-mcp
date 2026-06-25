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

        ### Reference dereferencing (critical)
        Do NOT compare reference attributes directly against GUID strings in XPath.
        WRONG: /BindingDescription[BoundObjectType = '6cb7e506-...']
        RIGHT: /BindingDescription[BoundObjectType = /ObjectTypeDescription[Name = 'Person']]
        Always use the dereferencing syntax: ReferenceAttr = /TargetType[UniqueAttr = 'value']

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

        ## Sets
        Sets define resource collections used in policy. They have:
        - Filter: XPath-based dynamic membership (wrapped in a <Filter> XML element with dialect namespace)
        - ExplicitMember: manually added references
        - ComputedMember: read-only union of both
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
