## Object Explorer Service

> Object Explorer  service provides functionality to retrieve the hierarchical objects in each instance of SQL Server. It handles requests to expand or refresh a node in the hierarchy.

> The service uses generated classes to create the objects hierarchy and there are two xml files used as sources to generated the classes.

### TreeNodeDefinition.xml
> TreeNodeDefinition.xml defines all the hierarchies and all the supported objects types. It includes:

* The hierarchy of the SQL objects
* The supported objects for each version of SQL Server.
* How to filter objects based on the properties in each node
* How to query each object (Reference to another generated code to query each object type)

### SmoQueryModelDefinition.xml
> SmoQueryModelDefinition.xml defines the supported object types and how to query each type using SMO library. ChildQuerierTypes attribute in TreeNodeDefinition.xml nodes has reference to the types in this xml file. It includes:

* List of types that are defined in SMO library
* Name of the parent and the field to query each type

### Query optimization 
    To get each object type, SMO by default only gets the name and schema and not all it's properties. 
	To optimize the query to get the properties needed to create each node, add the properties to the node element in TreeNodeDefinition.xml. 
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

### How to add a new SQL object type
To add a new object type, 
* Add the type to TreeNodeDefinition.xml and SmoQueryModelDefinition.xml. 
* Regenerate the classes by running Build.cmd/build.sh -target=CodeGen






