// Helpers for reading XLIFF (.xlf) localization files and writing .resx output.
// Used by the SRGen task in build.cake.

using System.Xml.Linq;

var xliffNamespace = (XNamespace)"urn:oasis:names:tc:xliff:document:1.2";
var xmlNamespace = (XNamespace)"http://www.w3.org/XML/1998/namespace";

/// <summary>
/// Rewrites a text file in place with CRLF line endings so that generated
/// localization files are identical regardless of the platform running the build.
/// </summary>
void NormalizeToCrlf(string filePath)
{
    if (!System.IO.File.Exists(filePath)) return;
    var content = System.IO.File.ReadAllText(filePath, System.Text.Encoding.UTF8);
    // Strip UTF-8 BOM character if SRGen emitted one — committed files don't have it.
    if (content.Length > 0 && content[0] == '\uFEFF')
        content = content.Substring(1);
    var normalized = content.Replace("\r\n", "\n").Replace("\n", "\r\n");
    // Write with explicit no-BOM UTF-8 so the encoder doesn't re-add the preamble.
    System.IO.File.WriteAllText(filePath, normalized, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

XElement CreateResHeader(string name, string value)
{
    return new XElement("resheader",
        new XAttribute("name", name),
        new XElement("value", value));
}

IEnumerable<XElement> GetTransUnits(XDocument document)
{
    return document
        .Descendants(xliffNamespace + "trans-unit")
        .Where(unit => unit.Attribute("id") != null);
}

/// <summary>
/// Copies each source string in an XLF file into its corresponding target element,
/// marking each target as state="new". Creates target elements where missing.
/// Saves the result back to the same file in place.
/// </summary>
void UpdateXlfTargetsFromSource(string xlfPath)
{
    var document = XDocument.Load(xlfPath, LoadOptions.PreserveWhitespace);
    foreach (var unit in GetTransUnits(document))
    {
        var source = unit.Element(xliffNamespace + "source");
        if (source == null)
        {
            continue;
        }

        var target = unit.Element(xliffNamespace + "target");
        if (target == null)
        {
            target = new XElement(xliffNamespace + "target");
            unit.Add(target);
        }

        target.Value = source.Value;
        target.SetAttributeValue("state", "new");
    }

    document.Save(xlfPath);
}

/// <summary>
/// Converts an XLF file to a .resx file, emitting the standard Microsoft ResX schema
/// comment and XSD block followed by one &lt;data&gt; element per trans-unit.
/// Only trans-units that already have a &lt;target&gt; element are included.
/// </summary>
void SaveXlfTargetsAsResx(string xlfPath, string resxPath)
{
    var xlfDocument = XDocument.Load(xlfPath, LoadOptions.PreserveWhitespace);
    var resxDocument = new XDocument(
        new XDeclaration("1.0", "utf-8", null),
        new XElement("root",
            new XComment(@"
    Microsoft ResX Schema

    Version 2.0

    The primary goals of this format is to allow a simple XML format
    that is mostly human readable. The generation and parsing of the
    various data types are done through the TypeConverter classes
    associated with the data types.

    Example:

    ... ado.net/XML headers & schema ...
    <resheader name=""resmimetype"">text/microsoft-resx</resheader>
    <resheader name=""version"">2.0</resheader>
    <resheader name=""reader"">System.Resources.ResXResourceReader, System.Windows.Forms, ...</resheader>
    <resheader name=""writer"">System.Resources.ResXResourceWriter, System.Windows.Forms, ...</resheader>
    <data name=""Name1""><value>this is my long string</value><comment>this is a comment</comment></data>
    <data name=""Color1"" type=""System.Drawing.Color, System.Drawing"">Blue</data>
    <data name=""Bitmap1"" mimetype=""application/x-microsoft.net.object.binary.base64"">
        <value>[base64 mime encoded serialized .NET Framework object]</value>
    </data>
    <data name=""Icon1"" type=""System.Drawing.Icon, System.Drawing"" mimetype=""application/x-microsoft.net.object.bytearray.base64"">
        <value>[base64 mime encoded string representing a byte array form of the .NET Framework object]</value>
        <comment>This is a comment</comment>
    </data>

    There are any number of ""resheader"" rows that contain simple
    name/value pairs.

    Each data row contains a name, and value. The row also contains a
    type or mimetype. Type corresponds to a .NET class that support
    text/value conversion through the TypeConverter architecture.
    Classes that don't support this are serialized and stored with the
    mimetype set.

    The mimetype is used for serialized objects, and tells the
    ResXResourceReader how to depersist the object. This is currently not
    extensible. For a given mimetype the value must be set accordingly:

    Note - application/x-microsoft.net.object.binary.base64 is the format
    that the ResXResourceWriter will generate, however the reader can
    read any of the formats listed below.

    mimetype: application/x-microsoft.net.object.binary.base64
    value   : The object must be serialized with
            : System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
            : and then encoded with base64 encoding.

    mimetype: application/x-microsoft.net.object.soap.base64
    value   : The object must be serialized with
            : System.Runtime.Serialization.Formatters.Soap.SoapFormatter
            : and then encoded with base64 encoding.
    mimetype: application/x-microsoft.net.object.bytearray.base64
    value   : The object must be serialized into a byte array
            : using a System.ComponentModel.TypeConverter
            : and then encoded with base64 encoding.
    "),
            XElement.Parse(@"<xsd:schema id=""root"" xmlns="""" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
    <xsd:import namespace=""http://www.w3.org/XML/1998/namespace"" />
    <xsd:element name=""root"" msdata:IsDataSet=""true"">
      <xsd:complexType>
        <xsd:choice maxOccurs=""unbounded"">
          <xsd:element name=""metadata"">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" />
              </xsd:sequence>
              <xsd:attribute name=""name"" use=""required"" type=""xsd:string"" />
              <xsd:attribute name=""type"" type=""xsd:string"" />
              <xsd:attribute name=""mimetype"" type=""xsd:string"" />
              <xsd:attribute ref=""xml:space"" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name=""assembly"">
            <xsd:complexType>
              <xsd:attribute name=""alias"" type=""xsd:string"" />
              <xsd:attribute name=""name"" type=""xsd:string"" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name=""data"">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />
                <xsd:element name=""comment"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""2"" />
              </xsd:sequence>
              <xsd:attribute name=""name"" type=""xsd:string"" use=""required"" msdata:Ordinal=""1"" />
              <xsd:attribute name=""type"" type=""xsd:string"" msdata:Ordinal=""3"" />
              <xsd:attribute name=""mimetype"" type=""xsd:string"" msdata:Ordinal=""4"" />
              <xsd:attribute ref=""xml:space"" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name=""resheader"">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />
              </xsd:sequence>
              <xsd:attribute name=""name"" type=""xsd:string"" use=""required"" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>"),
            CreateResHeader("resmimetype", "text/microsoft-resx"),
            CreateResHeader("version", "2.0"),
            CreateResHeader("reader", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
            CreateResHeader("writer", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")));

    var root = resxDocument.Root;
    foreach (var unit in GetTransUnits(xlfDocument))
    {
        var target = unit.Element(xliffNamespace + "target");
        if (target == null)
        {
            continue;
        }

        var data = new XElement("data",
            new XAttribute("name", unit.Attribute("id").Value),
            new XAttribute(xmlNamespace + "space", "preserve"),
            new XElement("value", target.Value));

        var note = unit.Element(xliffNamespace + "note");
        if (note != null && !string.IsNullOrWhiteSpace(note.Value))
        {
            data.Add(new XElement("comment", note.Value));
        }

        root.Add(data);
    }

    resxDocument.Save(resxPath);
}

/// <summary>
/// Normalises locale suffixes in a file name to canonical .NET culture casing
/// (e.g. zh-hans → zh-Hans, pt-br → pt-BR) so that case-sensitive file systems
/// do not end up with duplicate locale directories for the same culture.
/// </summary>
string CanonicalizeLocalizationFileName(string fileName)
{
    return fileName
        .Replace("pt-br", "pt-BR")
        .Replace("zh-hans", "zh-Hans")
        .Replace("zh-hant", "zh-Hant");
}
