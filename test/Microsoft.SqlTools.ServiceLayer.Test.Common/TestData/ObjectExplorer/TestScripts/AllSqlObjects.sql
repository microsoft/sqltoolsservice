
/****** Object:  User [amy0]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE USER [amy0] WITHOUT LOGIN WITH DEFAULT_SCHEMA=[dbo]
GO
/****** Object:  DatabaseRole [SalesManagers]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE ROLE [SalesManagers]
GO
/****** Object:  DatabaseRole [SalesPersons]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE ROLE [SalesPersons]
GO
ALTER ROLE [SalesManagers] ADD MEMBER [amy0]
GO
/****** Object:  Schema [Demo]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE SCHEMA [Demo]
GO
/****** Object:  Schema [HumanResources]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE SCHEMA [HumanResources]
GO
/****** Object:  Schema [Person]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE SCHEMA [Person]
GO
/****** Object:  FullTextCatalog [AW2014FullTextCatalog]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE FULLTEXT CATALOG [AW2014FullTextCatalog]WITH ACCENT_SENSITIVITY = ON
AS DEFAULT

GO
/****** Object:  XmlSchemaCollection [Person].[AdditionalContactInfoSchemaCollection]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE XML SCHEMA COLLECTION [Person].[AdditionalContactInfoSchemaCollection] AS N'<xsd:schema xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:t="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ContactInfo" targetNamespace="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ContactInfo"><xsd:element name="AdditionalContactInfo"><xsd:complexType mixed="true"><xsd:complexContent mixed="true"><xsd:restriction base="xsd:anyType"><xsd:sequence><xsd:any namespace="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ContactRecord http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ContactTypes" minOccurs="0" maxOccurs="unbounded" /></xsd:sequence></xsd:restriction></xsd:complexContent></xsd:complexType></xsd:element></xsd:schema><xsd:schema xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:t="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ContactRecord" targetNamespace="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ContactRecord"><xsd:element name="ContactRecord"><xsd:complexType mixed="true"><xsd:complexContent mixed="true"><xsd:restriction base="xsd:anyType"><xsd:choice minOccurs="0" maxOccurs="unbounded"><xsd:any namespace="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ContactTypes" /></xsd:choice><xsd:attribute name="date" type="xsd:date" /></xsd:restriction></xsd:complexContent></xsd:complexType></xsd:element></xsd:schema><xsd:schema xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:t="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ContactTypes" targetNamespace="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ContactTypes" elementFormDefault="qualified"><xsd:element name="eMail" type="t:eMailType" /><xsd:element name="facsimileTelephoneNumber" type="t:phoneNumberType" /><xsd:element name="homePostalAddress" type="t:addressType" /><xsd:element name="internationaliSDNNumber" type="t:phoneNumberType" /><xsd:element name="mobile" type="t:phoneNumberType" /><xsd:element name="pager" type="t:phoneNumberType" /><xsd:element name="physicalDeliveryOfficeName" type="t:addressType" /><xsd:element name="registeredAddress" type="t:addressType" /><xsd:element name="telephoneNumber" type="t:phoneNumberType" /><xsd:element name="telexNumber" type="t:phoneNumberType" /><xsd:complexType name="addressType"><xsd:complexContent><xsd:restriction base="xsd:anyType"><xsd:sequence><xsd:element name="Street" type="xsd:string" maxOccurs="2" /><xsd:element name="City" type="xsd:string" /><xsd:element name="StateProvince" type="xsd:string" /><xsd:element name="PostalCode" type="xsd:string" minOccurs="0" /><xsd:element name="CountryRegion" type="xsd:string" /><xsd:element name="SpecialInstructions" type="t:specialInstructionsType" minOccurs="0" /></xsd:sequence></xsd:restriction></xsd:complexContent></xsd:complexType><xsd:complexType name="eMailType"><xsd:complexContent><xsd:restriction base="xsd:anyType"><xsd:sequence><xsd:element name="eMailAddress" type="xsd:string" /><xsd:element name="SpecialInstructions" type="t:specialInstructionsType" minOccurs="0" /></xsd:sequence></xsd:restriction></xsd:complexContent></xsd:complexType><xsd:complexType name="phoneNumberType"><xsd:complexContent><xsd:restriction base="xsd:anyType"><xsd:sequence><xsd:element name="number"><xsd:simpleType><xsd:restriction base="xsd:string"><xsd:pattern value="[0-9\(\)\-]*" /></xsd:restriction></xsd:simpleType></xsd:element><xsd:element name="SpecialInstructions" type="t:specialInstructionsType" minOccurs="0" /></xsd:sequence></xsd:restriction></xsd:complexContent></xsd:complexType><xsd:complexType name="specialInstructionsType" mixed="true"><xsd:complexContent mixed="true"><xsd:restriction base="xsd:anyType"><xsd:sequence><xsd:any namespace="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ContactTypes" minOccurs="0" maxOccurs="unbounded" /></xsd:sequence></xsd:restriction></xsd:complexContent></xsd:complexType></xsd:schema>'
GO
/****** Object:  UserDefinedDataType [dbo].[AccountNumber]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE TYPE [dbo].[AccountNumber] FROM [nvarchar](15) NULL
GO
/****** Object:  UserDefinedDataType [dbo].[Flag]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE TYPE [dbo].[Flag] FROM [bit] NOT NULL
GO
/****** Object:  UserDefinedDataType [dbo].[Name]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE TYPE [dbo].[Name] FROM [nvarchar](50) NULL
GO
/****** Object:  UserDefinedDataType [dbo].[NameStyle]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE TYPE [dbo].[NameStyle] FROM [bit] NOT NULL
GO
/****** Object:  UserDefinedDataType [dbo].[OrderNumber]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE TYPE [dbo].[OrderNumber] FROM [nvarchar](25) NULL
GO
/****** Object:  UserDefinedDataType [dbo].[Phone]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE TYPE [dbo].[Phone] FROM [nvarchar](25) NULL
GO
/****** Object:  UserDefinedTableType [Sales].[SalesOrderDetailType_inmem]    Script Date: 4/27/2017 11:50:14 AM ******/
CREATE TYPE [Demo].[SalesOrderDetailType_inmem] AS TABLE(
	[OrderQty] [smallint] NOT NULL,
	[ProductID] [int] NOT NULL,
	[SpecialOfferID] [int] NOT NULL)
GO
/****** Object:  UserDefinedTableType [Sales].[SalesOrderDetailType_ondisk]    Script Date: 4/27/2017 11:50:15 AM ******/
CREATE TYPE [Demo].[SalesOrderDetailType_ondisk] AS TABLE(
	[OrderQty] [smallint] NOT NULL,
	[ProductID] [int] NOT NULL,
	[SpecialOfferID] [int] NOT NULL)
GO
/****** Object:  UserDefinedFunction [dbo].[fun1]    Script Date: 4/27/2017 11:50:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[fun1]
(
	-- Add the parameters for the function here
	
)
RETURNS int
AS
BEGIN
	-- Declare the return variable here
	
	-- Return the result of the function
	RETURN 1

END

GO
/****** Object:  UserDefinedFunction [dbo].[ufnGetContactInformation]    Script Date: 4/27/2017 11:50:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[ufnGetContactInformation](@PersonID int)
RETURNS @retContactInformation TABLE 
(
    -- Columns returned by the function
    [PersonID] int NOT NULL
)
AS 
-- Returns the first name, last name, job title and business entity type for the specified contact.
-- Since a contact can serve multiple roles, more than one row may be returned.
BEGIN
	INSERT INTO @retContactInformation (PersonID) values(1)
	RETURN;
END;

GO

/****** Object:  Table [HumanResources].[Employee_Temporal_History]    Script Date: 4/27/2017 12:02:08 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [HumanResources].[Employee_Temporal_History](
	[BusinessEntityID] [int] NOT NULL,
	[NationalIDNumber] [nvarchar](15) NOT NULL,
	[LoginID] [nvarchar](256) NOT NULL,
	[OrganizationNode] [hierarchyid] NULL,
	[OrganizationLevel] [smallint] NULL,
	[JobTitle] [nvarchar](50) NOT NULL,
	[BirthDate] [date] NOT NULL,
	[MaritalStatus] [nchar](1) NOT NULL,
	[Gender] [nchar](1) NOT NULL,
	[HireDate] [date] NOT NULL,
	[VacationHours] [smallint] NOT NULL,
	[SickLeaveHours] [smallint] NOT NULL,
	[ValidFrom] [datetime2](7) NOT NULL,
	[ValidTo] [datetime2](7) NOT NULL
) ON [PRIMARY]

GO


/****** Object:  Table [HumanResources].[Employee_Temporal]    Script Date: 4/27/2017 12:01:51 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [HumanResources].[Employee_Temporal](
	[BusinessEntityID] [int] NOT NULL,
	[NationalIDNumber] [nvarchar](15) NOT NULL,
	[LoginID] [nvarchar](256) NOT NULL,
	[OrganizationNode] [hierarchyid] NULL,
	[OrganizationLevel]  AS ([OrganizationNode].[GetLevel]()),
	[JobTitle] [nvarchar](50) NOT NULL,
	[BirthDate] [date] NOT NULL,
	[MaritalStatus] [nchar](1) NOT NULL,
	[Gender] [nchar](1) NOT NULL,
	[HireDate] [date] NOT NULL,
	[VacationHours] [smallint] NOT NULL,
	[SickLeaveHours] [smallint] NOT NULL,
	[ValidFrom] [datetime2](7) GENERATED ALWAYS AS ROW START NOT NULL,
	[ValidTo] [datetime2](7) GENERATED ALWAYS AS ROW END NOT NULL,
 CONSTRAINT [PK_Employee_History_BusinessEntityID] PRIMARY KEY CLUSTERED 
(
	[BusinessEntityID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
	PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo])
) ON [PRIMARY]
WITH
(
SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [HumanResources].[Employee_Temporal_History] )
)

GO

/****** Object:  Table [HumanResources].[Employee_Ledger]    Script Date: 7/19/2022 01:28:00 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [HumanResources].[Employee_Ledger](
	[BusinessEntityID] [int] NOT NULL,
	[NationalIDNumber] [nvarchar](15) NOT NULL,
	[LoginID] [nvarchar](256) NOT NULL,
	[OrganizationNode] [hierarchyid] NULL,
	[OrganizationLevel]  AS ([OrganizationNode].[GetLevel]()),
	[JobTitle] [nvarchar](50) NOT NULL,
	[BirthDate] [date] NOT NULL,
	[MaritalStatus] [nchar](1) NOT NULL,
	[Gender] [nchar](1) NOT NULL,
	[HireDate] [date] NOT NULL,
	[VacationHours] [smallint] NOT NULL,
	[SickLeaveHours] [smallint] NOT NULL,
)WITH (
	LEDGER = ON,
	SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [HumanResources].[Employee_Ledger_History] )
)

GO

/****** Object:  Table [HumanResources].[Employee_Ledger_AppendOnly]    Script Date: 7/19/2022 01:28:00 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [HumanResources].[Employee_Ledger_AppendOnly](
	[BusinessEntityID] [int] NOT NULL,
	[NationalIDNumber] [nvarchar](15) NOT NULL,
	[LoginID] [nvarchar](256) NOT NULL,
	[OrganizationNode] [hierarchyid] NULL,
	[OrganizationLevel]  AS ([OrganizationNode].[GetLevel]()),
	[JobTitle] [nvarchar](50) NOT NULL,
	[BirthDate] [date] NOT NULL,
	[MaritalStatus] [nchar](1) NOT NULL,
	[Gender] [nchar](1) NOT NULL,
	[HireDate] [date] NOT NULL,
	[SalariedFlag] [dbo].[Flag] NOT NULL,
	[VacationHours] [smallint] NOT NULL,
	[SickLeaveHours] [smallint] NOT NULL,
	[CurrentFlag] [dbo].[Flag] NOT NULL,
	[rowguid] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[ModifiedDate] [datetime] NOT NULL,
)WITH (
	LEDGER = ON (APPEND_ONLY = ON)
)

GO

/****** Object:  Table [HumanResources].[Ledger_For_Drop]    Script Date: 7/19/2022 01:28:00 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [HumanResources].[Ledger_For_Drop](
	[BusinessEntityID] [int] NOT NULL,
	[NationalIDNumber] [nvarchar](15) NOT NULL,
	[LoginID] [nvarchar](256) NOT NULL,
	[OrganizationNode] [hierarchyid] NULL
)WITH (
	LEDGER = ON,
	SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [HumanResources].[Ledger_For_Drop_History] )
)

GO

DROP TABLE [HumanResources].[Ledger_For_Drop]

GO

/****** Object:  Table [HumanResources].[Employee]    Script Date: 4/27/2017 11:50:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HumanResources].[Employee](
	[BusinessEntityID] [int] NOT NULL,
	[NationalIDNumber] [nvarchar](15) NOT NULL,
	[LoginID] [nvarchar](256) NOT NULL,
	[OrganizationNode] [hierarchyid] NULL,
	[OrganizationLevel]  AS ([OrganizationNode].[GetLevel]()),
	[JobTitle] [nvarchar](50) NOT NULL,
	[BirthDate] [date] NOT NULL,
	[MaritalStatus] [nchar](1) NOT NULL,
	[Gender] [nchar](1) NOT NULL,
	[HireDate] [date] NOT NULL,
	[SalariedFlag] [dbo].[Flag] NOT NULL,
	[VacationHours] [smallint] NOT NULL,
	[SickLeaveHours] [smallint] NOT NULL,
	[CurrentFlag] [dbo].[Flag] NOT NULL,
	[rowguid] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[ModifiedDate] [datetime] NOT NULL,
 CONSTRAINT [PK_Employee_BusinessEntityID] PRIMARY KEY CLUSTERED 
(
	[BusinessEntityID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [Person].[Person]    Script Date: 4/27/2017 11:50:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Person].[Person](
	[BusinessEntityID] [int] NOT NULL,
	[PersonType] [nchar](2) NOT NULL,
	[NameStyle] [dbo].[NameStyle] NOT NULL,
	[Title] [nvarchar](8) NULL,
	[FirstName] [dbo].[Name] NOT NULL,
	[MiddleName] [dbo].[Name] NULL,
	[LastName] [dbo].[Name] NOT NULL,
	[Suffix] [nvarchar](10) NULL,
	[EmailPromotion] [int] NOT NULL,
	[AdditionalContactInfo] [xml](CONTENT [Person].[AdditionalContactInfoSchemaCollection]) NULL,
	[rowguid] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[ModifiedDate] [datetime] NOT NULL,
 CONSTRAINT [PK_Person_BusinessEntityID] PRIMARY KEY CLUSTERED 
(
	[BusinessEntityID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  View [HumanResources].[vEmployee]    Script Date: 4/27/2017 11:50:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [HumanResources].[vEmployee] 
AS 
SELECT 
    e.[BusinessEntityID]
    ,p.[Title]
    ,p.[FirstName]
    ,p.[MiddleName]
    ,p.[LastName]
    ,p.[Suffix]
    ,e.[JobTitle]  
   
    ,p.[AdditionalContactInfo]
FROM [HumanResources].[Employee] e
	INNER JOIN [Person].[Person] p
	ON p.[BusinessEntityID] = e.[BusinessEntityID]
    

GO

CREATE TRIGGER ViewTrigger on [HumanResources].[vEmployee] 
INSTEAD OF INSERT
AS
BEGIN
Update [HumanResources].[Employee] 
   set JobTitle = 'Test1'
   Where JobTitle = 'Test'
END
GO

CREATE TABLE [dbo].[tableWithAllDataTypes](
	[cDecimal] [decimal](18, 5) NULL,
	[cNumeric] [numeric](18, 2) NULL,
	[cBigint] [bigint] NULL,
	[cDate] [date] NULL,
	[cDatetime] [datetime] NULL,
	[cFloat] [float] NULL,
	[cSmalldatetime] [smalldatetime] NULL,
	[cDatetime2] [datetime2](7) NULL,
	[cDatetimeoffset] [datetimeoffset](7) NULL,
	[cTime] [time](7) NULL,
	[cBinary] [Binary] NULL,
	[cBit] [Bit] NULL,
	[cChar] [Char] NULL,
	[cMoney] [Money] NULL
) ON [PRIMARY]
GO
ALTER TABLE [HumanResources].[Employee] ADD  CONSTRAINT [DF_Employee_SalariedFlag]  DEFAULT ((1)) FOR [SalariedFlag]
GO
ALTER TABLE [HumanResources].[Employee] ADD  CONSTRAINT [DF_Employee_VacationHours]  DEFAULT ((0)) FOR [VacationHours]
GO
ALTER TABLE [HumanResources].[Employee] ADD  CONSTRAINT [DF_Employee_SickLeaveHours]  DEFAULT ((0)) FOR [SickLeaveHours]
GO
ALTER TABLE [HumanResources].[Employee] ADD  CONSTRAINT [DF_Employee_CurrentFlag]  DEFAULT ((1)) FOR [CurrentFlag]
GO
ALTER TABLE [HumanResources].[Employee] ADD  CONSTRAINT [DF_Employee_rowguid]  DEFAULT (newid()) FOR [rowguid]
GO
ALTER TABLE [HumanResources].[Employee] ADD  CONSTRAINT [DF_Employee_ModifiedDate]  DEFAULT (getdate()) FOR [ModifiedDate]
GO
ALTER TABLE [Person].[Person] ADD  CONSTRAINT [DF_Person_NameStyle]  DEFAULT ((0)) FOR [NameStyle]
GO
ALTER TABLE [Person].[Person] ADD  CONSTRAINT [DF_Person_EmailPromotion]  DEFAULT ((0)) FOR [EmailPromotion]
GO
ALTER TABLE [Person].[Person] ADD  CONSTRAINT [DF_Person_rowguid]  DEFAULT (newid()) FOR [rowguid]
GO
ALTER TABLE [Person].[Person] ADD  CONSTRAINT [DF_Person_ModifiedDate]  DEFAULT (getdate()) FOR [ModifiedDate]
GO
ALTER TABLE [HumanResources].[Employee]  WITH CHECK ADD  CONSTRAINT [FK_Employee_Person_BusinessEntityID] FOREIGN KEY([BusinessEntityID])
REFERENCES [Person].[Person] ([BusinessEntityID])
GO
ALTER TABLE [HumanResources].[Employee] CHECK CONSTRAINT [FK_Employee_Person_BusinessEntityID]
GO
ALTER TABLE [HumanResources].[Employee]  WITH CHECK ADD  CONSTRAINT [CK_Employee_BirthDate] CHECK  (([BirthDate]>='1930-01-01' AND [BirthDate]<=dateadd(year,(-18),getdate())))
GO
ALTER TABLE [HumanResources].[Employee] CHECK CONSTRAINT [CK_Employee_BirthDate]
GO
ALTER TABLE [HumanResources].[Employee]  WITH CHECK ADD  CONSTRAINT [CK_Employee_Gender] CHECK  ((upper([Gender])='F' OR upper([Gender])='M'))
GO
ALTER TABLE [HumanResources].[Employee] CHECK CONSTRAINT [CK_Employee_Gender]
GO
ALTER TABLE [HumanResources].[Employee]  WITH CHECK ADD  CONSTRAINT [CK_Employee_HireDate] CHECK  (([HireDate]>='1996-07-01' AND [HireDate]<=dateadd(day,(1),getdate())))
GO
ALTER TABLE [HumanResources].[Employee] CHECK CONSTRAINT [CK_Employee_HireDate]
GO
ALTER TABLE [HumanResources].[Employee]  WITH CHECK ADD  CONSTRAINT [CK_Employee_MaritalStatus] CHECK  ((upper([MaritalStatus])='S' OR upper([MaritalStatus])='M'))
GO
ALTER TABLE [HumanResources].[Employee] CHECK CONSTRAINT [CK_Employee_MaritalStatus]
GO
ALTER TABLE [HumanResources].[Employee]  WITH CHECK ADD  CONSTRAINT [CK_Employee_SickLeaveHours] CHECK  (([SickLeaveHours]>=(0) AND [SickLeaveHours]<=(120)))
GO
ALTER TABLE [HumanResources].[Employee] CHECK CONSTRAINT [CK_Employee_SickLeaveHours]
GO
ALTER TABLE [HumanResources].[Employee]  WITH CHECK ADD  CONSTRAINT [CK_Employee_VacationHours] CHECK  (([VacationHours]>=(-40) AND [VacationHours]<=(240)))
GO
ALTER TABLE [HumanResources].[Employee] CHECK CONSTRAINT [CK_Employee_VacationHours]
GO
ALTER TABLE [Person].[Person]  WITH CHECK ADD  CONSTRAINT [CK_Person_EmailPromotion] CHECK  (([EmailPromotion]>=(0) AND [EmailPromotion]<=(2)))
GO
ALTER TABLE [Person].[Person] CHECK CONSTRAINT [CK_Person_EmailPromotion]
GO
ALTER TABLE [Person].[Person]  WITH CHECK ADD  CONSTRAINT [CK_Person_PersonType] CHECK  (([PersonType] IS NULL OR (upper([PersonType])='GC' OR upper([PersonType])='SP' OR upper([PersonType])='EM' OR upper([PersonType])='IN' OR upper([PersonType])='VC' OR upper([PersonType])='SC')))
GO
ALTER TABLE [Person].[Person] CHECK CONSTRAINT [CK_Person_PersonType]
GO
/****** Object:  StoredProcedure [HumanResources].[sp_GetEmployee_Person_Info_AsOf]    Script Date: 4/27/2017 11:50:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
	Stored procedure used for querying Employee and Person data AS OF
	If @AsOf parameter is NULL, current data is queried
	otherwise both current and historical data is queried
*/
CREATE  PROCEDURE [HumanResources].[sp_GetEmployee_Person_Info_AsOf]
@asOf datetime2 = NULL
AS
SELECT * FROM [HumanResources].[Employee]

GO


CREATE TRIGGER [Trigger_2] 
ON DATABASE 
FOR DROP_TABLE
AS 
   SELECT COUNT(Person.FirstName) from Person.Person
   RAISERROR ('You must disable Trigger "Trigger_1" to drop synonyms!',10, 1)
   ROLLBACK

GO

CREATE SYNONYM MyProduct  
FOR Person.Person;  
GO  


CREATE RULE list_rule  
AS   
@list IN ('1389', '0736', '0877');  

GO


CREATE DEFAULT phonedflt AS 'unknown';  
GO

CREATE SEQUENCE Demo.ID_Seq  
    START WITH 24329  
    INCREMENT BY 1 ;  
GO  


CREATE FUNCTION dbo.ufnGetInventoryStock(@ProductID int)  
RETURNS int   
AS   
-- Returns the stock level for the product.  
BEGIN  
    DECLARE @ret int;  
    SELECT @ret = SUM(p.BusinessEntityID)   
    FROM Person.Person p   
   
     IF (@ret IS NULL)   
        SET @ret = 0;  
    RETURN @ret;  
END;  
GO  

--EXEC sp_addmessage @msgnum = 50005,
--          @severity = 16,
--          @msgtext = N'%s';
--GO


CREATE TRIGGER TableTrigger
   ON  Person.Person
   AFTER INSERT
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for trigger here

END
GO


/****** Object:  Index [NonClusteredIndex-Login]    Script Date: 4/27/2017 2:55:49 PM ******/
CREATE NONCLUSTERED INDEX [NonClusteredIndex-Login] ON [HumanResources].[Employee]
(
	[LoginID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/****** Column Set ***/
CREATE TABLE tableWithColumnset (i int SPARSE, cs xml column_set FOR ALL_SPARSE_COLUMNS); 
GO


/****** Procedure With output parameter ***/
CREATE PROCEDURE uspGetList @Product varchar(40)   
    , @MaxPrice money 
    , @ComparePrice money OUTPUT  
    , @ListPrice money OUT  
AS  
   select 1
-- Populate the output variable @compareprice.  
SET @ComparePrice = @MaxPrice;  
GO  