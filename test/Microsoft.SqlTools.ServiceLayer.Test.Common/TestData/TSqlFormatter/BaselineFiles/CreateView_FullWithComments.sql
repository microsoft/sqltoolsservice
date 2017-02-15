CREATE VIEW my_schema.my_view_name
--and
(
    /* we */
    column1,
    column2,
    /* can */
    column3
)
-- put
WITH
    /* comments */
    /*even multiple ones */
    -- and of various types
    SCHEMABINDING,
    -- everywhere
    ENCRYPTION,
    -- we
    VIEW_METADATA
/* want*/
AS
    /* because */
    SELECT *
    FROM mytable
-- it's SQL