-- Fabric Warehouse (SqlDwUnified) source fixture covering the supported data types
-- exercised by SchemaCompareFabricTests. Keep types confined to the Fabric-supported
-- list from
-- https://learn.microsoft.com/en-us/fabric/data-warehouse/data-types
-- so the patched-DSP dacpac built from this script loads under SqlDwUnified without
-- DacFx complaining about unsupported types.
CREATE TABLE [dbo].[AllTypes]
(
    [BitCol] BIT NOT NULL,
    [SmallIntCol] SMALLINT NOT NULL,
    [IntCol] INT NOT NULL,
    [BigIntCol] BIGINT NOT NULL,
    [DecimalCol] DECIMAL(10, 2) NOT NULL,
    [NumericCol] NUMERIC(10, 2) NOT NULL,
    [FloatCol] FLOAT(53) NOT NULL,
    [RealCol] REAL NOT NULL,
    [DateCol] DATE NOT NULL,
    [TimeCol] TIME(6) NOT NULL,
    [DateTime2Col] DATETIME2(6) NOT NULL,
    [CharCol] CHAR(10) NOT NULL,
    [VarCharCol] VARCHAR(100) NOT NULL,
    [VarBinaryCol] VARBINARY(100) NULL,
    [UniqueIdentifierCol] UNIQUEIDENTIFIER NOT NULL
);
