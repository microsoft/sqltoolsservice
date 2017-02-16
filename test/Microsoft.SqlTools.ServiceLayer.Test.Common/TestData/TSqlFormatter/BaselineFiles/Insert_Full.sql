with
    my_initial_table( column1, column2 )
    AS
    (
        select *
        from mytable
    ),
    my_other_table( column1, column2 )
    AS
    (
        select *
        from mytable
    )
Insert top (10) PERCENT
into myserver.mydatabase.myschema.mytable_or_view WITH (TABLOCK)
    ( col1, col2, col3, col4, col5 )
VALUES
    ( DEFault, NULL, 1, N'My Value', 'Today'),
    ( 45, 5, 1, N'My Last Value', 'Yesterday'),
    ( 8, 6, 1, N'My Next Value', 'Tomorrow')