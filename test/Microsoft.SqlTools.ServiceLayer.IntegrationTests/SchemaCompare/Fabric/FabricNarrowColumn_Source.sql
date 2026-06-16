-- Source for the narrowing / type-incompatible ALTER COLUMN scenario. Pair with
-- FabricNarrowColumn_Target.sql: BIGINT→INT and VARCHAR(200)→VARCHAR(10). DacFx is
-- expected to emit ALTER COLUMN diffs accompanied by data-loss warnings.
CREATE TABLE [dbo].[NarrowColumns]
(
    [Id] INT NOT NULL,
    [WideCount] BIGINT NOT NULL,
    [LongName] VARCHAR(200) NOT NULL
);
