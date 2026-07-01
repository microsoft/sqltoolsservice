-- Source for the widening (compatible) ALTER COLUMN scenario. Pair with
-- FabricWidenColumn_Target.sql: columns are widened smallint→int, int→bigint,
-- varchar(50)→varchar(200), decimal(10,2)→decimal(18,4). DacFx should emit
-- ALTER COLUMN diffs without data-loss warnings.
CREATE TABLE [dbo].[WidenColumns]
(
    [Id] INT NOT NULL,
    [TinyCount] SMALLINT NOT NULL,
    [SmallCount] INT NOT NULL,
    [Code] VARCHAR(50) NOT NULL,
    [Amount] DECIMAL(10, 2) NOT NULL
);
