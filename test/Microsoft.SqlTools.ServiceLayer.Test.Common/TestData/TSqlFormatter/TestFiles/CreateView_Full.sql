CREATE VIEW my_schema.my_view_name
(column1,  column2,
    column3
)
WITH SCHEMABINDING, ENCRYPTION,VIEW_METADATA
AS SELECT * FROM  mytable
