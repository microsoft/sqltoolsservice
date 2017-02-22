CREATE PROCEDURE au_info
    @lastname varchar(40),
    @firstname varchar(20)
AS
SELECT au_lname, au_fname, title, pub_name
FROM authors a INNER JOIN titleauthor ta
    ON a.au_id = ta.au_id INNER JOIN titles t
    ON t.title_id = ta.title_id INNER JOIN publishers p
    ON t.pub_id = p.pub_id
WHERE  au_fname = @firstname
    AND au_lname = @lastname
GO
