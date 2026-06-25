# MIM Service MCP Server

An [MCP](https://modelcontextprotocol.io) server that exposes Microsoft Identity Manager (MIM) Service operations as tools for AI agents. It wraps the [Lithnet.ResourceManagement.Client](https://github.com/lithnet/resourcemanagement-client) library and runs as a stdio-based MCP server that Claude Code, Codex, and other MCP-compatible tools can call directly.

## Installation

Install as a .NET global tool:

```
dotnet tool install -g Lithnet.ResourceManagement.McpServer
```

## Configuration

Register the server in your MCP client. For Claude Code, add it to `~/.claude.json` under `mcpServers`:

```json
{
  "mcpServers": {
    "mim": {
      "type": "stdio",
      "command": "mim-mcp",
      "args": [],
      "env": {
        "MIM_BASE_ADDRESS": "http://mimserver"
      }
    }
  }
}
```

Restart Claude Code after adding the configuration. The tools will appear as `mcp__mim__*`.

### Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `MIM_BASE_ADDRESS` | `http://localhost:5725` | MIM Service URL |
| `MIM_CONNECTION_MODE` | `Auto` | Connection mode: `Auto`, `DirectNetTcp`, `LocalProxy`, or `RemoteProxy` |
| `MIM_USERNAME` | *(current user)* | Optional explicit credentials in `DOMAIN\user` format. When not set, the current user's Kerberos credentials are used. |
| `MIM_PASSWORD` | *(none)* | Password for explicit credentials |
| `MIM_SPN` | *(derived from hostname)* | Kerberos service principal name override |
| `MIM_RMC_HOST_EXE` | *(auto-discovered)* | Path to the .NET Framework proxy executable. Only used with `LocalProxy` mode. |

### Connection modes

The MIM Service uses WCF, which is not available on .NET Core/.NET 5+. The underlying client library handles this transparently, but the right mode depends on where the MCP server is running relative to the MIM Service.

For a full explanation of each mode, when to use it, and how to set it up, see the [Connection guide](https://github.com/lithnet/resourcemanagement-client/wiki/Connection-guide) in the client library wiki.

In most cases, leave `MIM_CONNECTION_MODE` set to `Auto` and let the client detect the correct mode.

## Tools

The server exposes 9 tools.

### search_resources

Search for resources using a raw XPath filter or structured filters.

Supports two modes:
- **XPath mode**: pass a raw XPath expression (e.g. `/Person[Domain = 'CORP']`)
- **Structured mode**: pass `objectType`, `filters` (array of attribute/operator/value conditions), and `filterOperator` (`And` or `Or`)

The `attributes` parameter is required. Specify which attributes to return.

### get_resource

Get a single resource by ObjectID (GUID) or by key (object type + attribute name + value).

### create_resource

Create a new resource with the specified object type and attributes.

### update_resource

Modify attributes on an existing resource. Supports three operations:
- `attributes`: set/replace values
- `addValues`: add values to multi-valued attributes
- `removeValues`: remove values from multi-valued attributes

### delete_resource

Delete a resource by ObjectID.

### get_schema_object_types

List all object types defined in the MIM Service, with name, display name, and description.

### get_schema_attributes

Get the attributes bound to an object type, including data type, multivalued flag, required flag, and validation regex.

### get_rcdc

Get the RCDC (Resource Control Display Configuration) XML for an object type. Optionally filter by mode (`create`, `edit`, or `view`).

### refresh_schema

Reload the client's cached schema from the MIM Service. Call this after creating or modifying `AttributeTypeDescription`, `BindingDescription`, or `ObjectTypeDescription` resources. Without this, `get_schema_attributes` and `get_schema_object_types` will return stale data.

## Reference attribute handling

Reference attributes (attributes that point to other resources) are returned as GUID strings. To resolve what a reference points to, call `get_resource` with that GUID as the `objectId`.

## Requirements

- .NET 10.0 or later
- Network access to a MIM Service instance
- Appropriate Kerberos or explicit credentials for the MIM Service

## How can I contribute to the project?

* Found an issue? [Log it](https://github.com/lithnet/resourcemanagement-mcp/issues)
* Want to fix an issue or add functionality? Clone the project and submit a pull request

## Keep up to date

* [Visit our blog](http://blog.lithnet.io)
