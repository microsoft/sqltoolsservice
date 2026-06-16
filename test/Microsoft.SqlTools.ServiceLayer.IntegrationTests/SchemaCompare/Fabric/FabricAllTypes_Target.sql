-- Pair with FabricAllTypes_Source.sql but with one column removed (UniqueIdentifierCol)
-- so the comparison produces a real difference. Asserts (a) all supported types load
-- under SqlDwUnified without DSP rejection, and (b) the comparison detects the
-- missing column as an Object/Change diff.
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
    [VarBinaryCol] VARBINARY(100) NULL
);
