CREATE TABLE [Person].[Address] (
 [AddressID]       INT              IDENTITY (1, 1) NOT FOR REPLICATION NOT NULL,
   CONSTRAINT [PK_Address_AddressID] PRIMARY KEY CLUSTERED ([AddressID] ASC),[AddressLine1]    NVARCHAR (60)    NOT NULL,
);
--closing comment