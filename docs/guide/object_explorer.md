# Object Explorer Service

Object Explorer  service provides functionality to retrieve the hierarchical objects in each instance of SQL Server. It handles requests to expand or refresh a node in the hierarchy.

The service uses generated classes to create the objects hierarchy and there are two xml files used as sources to generated the classes.

## Key Files

### SmoTreeNodesDefinition.xml

SmoTreeNodesDefinition.xml defines all the hierarchies and all the supported objects types. It includes:

* The hierarchy of the SQL objects
* The supported objects for each version of SQL Server.
* How to filter objects based on the properties in each node
* How to query each object (Reference to another generated code to query each object type)

### SmoQueryModelDefinition.xml

SmoQueryModelDefinition.xml defines the supported object types and how to query each type using SMO library. ChildQuerierTypes attribute in SmoTreeNodesDefinition.xml nodes has reference to the types in this xml file. It includes:

* List of types that are defined in SMO library
* Name of the parent and the field to query each type

## Query optimization
To get each object type, SMO by default only gets the name and schema and not all its properties.

To optimize the query to get the properties needed to create each node, add the properties to the node element in SmoTreeNodesDefinition.xml.

For example, to get the table node, we also need to get two properties IsSystemVersioned and TemporalType which are included as properties in the table node:

### Sample

```xml
<Node Name="Tables" LocLabel="SR.SchemaHierarchy_Tables" BaseClass="ModelBased" Strategy="MultipleElementsOfType" ChildQuerierTypes="SqlTable" TreeNode="TableTreeNode">
    <Filters >
      <Filter Property="IsSystemObject" Value="0" Type="bool" />
      <Filter Property="TemporalType" Type="Enum" ValidFor="Sql2016|Sql2017|AzureV12">
        <Value>TableTemporalType.None</Value>
        <Value>TableTemporalType.SystemVersioned</Value>
      </Filter>
    </Filters>
    <Properties>
      <Property Name="IsSystemVersioned" ValidFor="Sql2016|Sql2017|AzureV12"/>
	  <Property Name="TemporalType" ValidFor="Sql2016|Sql2017|AzureV12"/>
    </Properties>
    <Child Name="SystemTables" IsSystemObject="1"/>
</Node>
```

## Guides
### Add a new SQL object type

1. Add the type to SmoTreeNodesDefinition.xml and SmoQueryModelDefinition.xml.
2. Regenerate the classes by running `build.[cmd|sh] -target=CodeGen`

### Add new SQL Server Type/Version

1. Add friendly names for the type to the [SqlServerType enum](https://github.com/Microsoft/sqltoolsservice/blob/main/src/Microsoft.SqlTools.ServiceLayer/ObjectExplorer/SqlServerType.cs)
2. Update [CalculateServerType](https://github.com/Microsoft/sqltoolsservice/blob/main/src/Microsoft.SqlTools.ServiceLayer/ObjectExplorer/SqlServerType.cs) method to support calculating the new server type
3. Update [ValidForFlag](https://github.com/Microsoft/sqltoolsservice/blob/main/src/Microsoft.SqlTools.ServiceLayer/ObjectExplorer/ValidForFlag.cs) with the new type, adding it to the flag unions as necessary.
4. Update [SmoTreeNodesDefinition.xml](https://github.com/Microsoft/sqltoolsservice/blob/main/src/Microsoft.SqlTools.ServiceLayer/ObjectExplorer/SmoModel/SmoTreeNodesDefinition.xml) - adding the new type to any ValidFor fields that are supported on the new type.
5. Run `build.[cmd|sh] -target=CodeGen` to regenerate the class definition file (note that you may not see any changes, this is expected unless folder nodes or property fields are being updated)
6. Build and verify that your changes result in the expected nodes being shown when connected to a server of the new type






