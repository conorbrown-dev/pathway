# Unit tests for the T-SQL front-end in src/sql-graders/_tsql.py. Run by tsql-engine.test.mjs (and
# directly: python3 -I test/tsql_engine_test.py).
import os
import sys
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "src", "sql-graders"))
import _tsql as t  # noqa: E402

SHOP = """
CREATE TABLE Customers (
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    City VARCHAR(30) NULL,
    Active BIT NOT NULL DEFAULT 1
);
CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) CONSTRAINT PK_Orders PRIMARY KEY,
    CustomerId INT NOT NULL REFERENCES dbo.Customers(CustomerId),
    Total DECIMAL(10,2) NOT NULL,
    OrderDate DATE NOT NULL
);
INSERT INTO dbo.Customers (Name, City) VALUES ('Ada', 'London'), ('Grace', 'New York'), (N'Linus', NULL);
INSERT INTO Orders (CustomerId, Total, OrderDate) VALUES
  (1, 10.50, '2026-01-05'), (1, 20.00, '2026-02-10'), (2, 5.25, '2026-02-11'), (2, 7.00, '2026-03-01');
"""


def shop():
    session = t.Session()
    session.run(SHOP)
    assert not session.errors, session.errors
    return session


class Q(unittest.TestCase):
    def setUp(self):
        self.s = shop()

    def rows(self, sql):
        return self.s.rows(sql)

    def error(self, sql):
        with self.assertRaises(t.TsqlError) as ctx:
            self.s.run(sql)
        return str(ctx.exception)

    # ----- the basics that must work -----
    def test_select_and_identity(self):
        self.assertEqual(self.rows("SELECT CustomerId, Name FROM Customers ORDER BY CustomerId;"), [[1, "Ada"], [2, "Grace"], [3, "Linus"]])

    def test_top_and_offset_fetch(self):
        self.assertEqual(self.rows("SELECT TOP (2) Name FROM Customers ORDER BY Name;"), [["Ada"], ["Grace"]])
        self.assertEqual(self.rows("SELECT TOP 1 Name FROM Customers ORDER BY Name DESC;"), [["Linus"]])
        self.assertEqual(self.rows("SELECT Name FROM Customers ORDER BY Name OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY;"), [["Grace"]])
        self.assertEqual(self.rows("SELECT Name FROM Customers ORDER BY Name OFFSET 2 ROWS;"), [["Linus"]])

    def test_case_insensitive_text_like_sql_server(self):
        self.assertEqual(self.rows("SELECT Name FROM Customers WHERE Name = 'ada';"), [["Ada"]])
        self.assertEqual(self.rows("SELECT Name FROM Customers WHERE City = 'LONDON';"), [["Ada"]])

    def test_null_logic(self):
        self.assertEqual(self.rows("SELECT Name FROM Customers WHERE City IS NULL;"), [["Linus"]])
        self.assertEqual(self.rows("SELECT Name FROM Customers WHERE City <> 'London' ORDER BY Name;"), [["Grace"]])

    def test_joins_and_aggregates(self):
        got = self.rows("""SELECT c.Name, COUNT(*) AS Orders, SUM(o.Total) AS Spent
                           FROM dbo.Customers AS c INNER JOIN Orders o ON o.CustomerId = c.CustomerId
                           GROUP BY c.Name HAVING SUM(o.Total) > 20 ORDER BY Spent DESC;""")
        self.assertEqual(got, [["Ada", 2, 30.5]])

    def test_left_join_null(self):
        got = self.rows("SELECT c.Name, o.OrderId FROM Customers c LEFT JOIN Orders o ON o.CustomerId = c.CustomerId WHERE o.OrderId IS NULL;")
        self.assertEqual(got, [["Linus", None]])

    def test_cte_and_window(self):
        got = self.rows(""";WITH Totals AS (SELECT CustomerId, SUM(Total) AS Spent FROM Orders GROUP BY CustomerId)
                          SELECT CustomerId, Spent, RANK() OVER (ORDER BY Spent DESC) AS Rnk FROM Totals ORDER BY Rnk;""")
        self.assertEqual(got, [[1, 30.5, 1], [2, 12.25, 2]])

    def test_cte_with_column_list(self):
        got = self.rows("WITH T (id, total) AS (SELECT CustomerId, SUM(Total) FROM Orders GROUP BY CustomerId) SELECT id FROM T ORDER BY id;")
        self.assertEqual(got, [[1], [2]])

    def test_string_concat_and_functions(self):
        self.assertEqual(self.rows("SELECT Name + ' of ' + ISNULL(City, 'nowhere') AS Who FROM Customers ORDER BY CustomerId;"),
                         [["Ada of London"], ["Grace of New York"], ["Linus of nowhere"]])
        self.assertEqual(self.rows("SELECT LEN('abc  '), LEFT('hello', 2), RIGHT('hello', 3), SUBSTRING('hello', 2, 3), CHARINDEX('l', 'hello');"), [[3, "he", "llo", "ell", 3]])
        self.assertEqual(self.rows("SELECT CONCAT('a', NULL, 'b'), UPPER('x'), REPLACE('aXa', 'x', '-');"), [["ab", "X", "a-a"]])
        self.assertEqual(self.rows("SELECT 'n=' + CAST(7 AS VARCHAR(5));"), [["n=7"]])

    def test_dates(self):
        self.assertEqual(self.rows("SELECT DATEADD(day, 3, '2026-01-30'), DATEADD(month, 1, '2026-01-31'), DATEDIFF(day, '2026-01-01', '2026-03-01');"),
                         [["2026-02-02", "2026-02-28", 59]])
        self.assertEqual(self.rows("SELECT YEAR('2026-05-06'), MONTH('2026-05-06'), DAY('2026-05-06'), EOMONTH('2026-02-10'), DATEDIFF(month, '2025-12-31', '2026-01-01');"), [[2026, 5, 6, "2026-02-28", 1]])

    def test_cast_rules(self):
        self.assertEqual(self.rows("SELECT CAST(12.9 AS INT), CAST('42' AS INT), TRY_CAST('abc' AS INT), CAST(2.345 AS DECIMAL(5,2));"), [[12, 42, None, 2.35]])
        self.s.run("SELECT CAST('abc' AS INT);")
        self.assertIn("Conversion failed", self.s.errors[-1])
        self.assertEqual(self.rows("SELECT CONVERT(INT, '5') + 1;"), [[6]])

    def test_string_agg(self):
        self.assertEqual(self.rows("SELECT STRING_AGG(Name, ', ') WITHIN GROUP (ORDER BY Name DESC) FROM Customers;"), [["Linus, Grace, Ada"]])
        self.assertEqual(self.rows("SELECT STRING_AGG(Name, '|') WITHIN GROUP (ORDER BY Name) FROM Customers;"), [["Ada|Grace|Linus"]])

    def test_variables_and_print(self):
        got = self.rows("DECLARE @min DECIMAL(10,2) = 6; SELECT OrderId FROM Orders WHERE Total > @min ORDER BY OrderId;")
        self.assertEqual(got, [[1], [2], [4]])
        self.s.run("DECLARE @n INT = 5; SET @n += 2; PRINT 'n is ' + CAST(@n AS VARCHAR(10));")
        self.assertEqual(self.s.messages[-1], "n is 7")

    def test_variable_reads_data_at_run_time(self):
        got = self.rows("DECLARE @n INT; INSERT INTO Orders (CustomerId, Total, OrderDate) VALUES (3, 1, '2026-04-01'); SET @n = (SELECT COUNT(*) FROM Orders); SELECT @n;")
        self.assertEqual(got, [[5]])

    def test_variable_scope_ends_at_go(self):
        self.s.run("DECLARE @x INT = 1;\nGO\nSELECT 1;") if False else None
        self.assertIn("Must declare the scalar variable", self.error("DECLARE @x INT = 1;\nGO\nSELECT @x;"))

    def test_dml_and_constraints(self):
        self.s.run("UPDATE Customers SET City = 'Paris' WHERE Name = 'Linus'; DELETE FROM Orders WHERE Total < 6;")
        self.assertEqual(self.rows("SELECT City FROM Customers WHERE Name = 'Linus';"), [["Paris"]])
        self.assertEqual(self.rows("SELECT COUNT(*) FROM Orders;"), [[3]])
        self.s.run("INSERT INTO Customers (Name) VALUES (NULL);")
        self.assertIn("Cannot insert the value NULL into column 'Name'", self.s.errors[-1])
        self.s.run("INSERT INTO Orders (CustomerId, Total, OrderDate) VALUES (99, 1, '2026-01-01');")
        self.assertIn("FOREIGN KEY", self.s.errors[-1])
        self.s.run("INSERT INTO Customers (Name) VALUES (REPLICATE('x', 60));")
        self.assertIn("truncated", self.s.errors[-1])

    def test_runtime_errors_continue_the_batch_like_sql_server(self):
        self.s.run("INSERT INTO Customers (Name) VALUES (NULL); INSERT INTO Customers (Name) VALUES ('Margaret');")
        self.assertEqual(len(self.s.errors), 1)
        self.assertEqual(self.rows("SELECT COUNT(*) FROM Customers WHERE Name = 'Margaret';"), [[1]])

    def test_syntax_error_stops_whole_batch(self):
        self.assertIn("Incorrect syntax", self.error("INSERT INTO Customers (Name) VALUES ('Zed'); SELEC 1;"))
        self.assertEqual(self.rows("SELECT COUNT(*) FROM Customers WHERE Name = 'Zed';"), [[0]])

    def test_transactions(self):
        self.s.run("BEGIN TRANSACTION; DELETE FROM Orders; ROLLBACK TRANSACTION;")
        self.assertEqual(self.rows("SELECT COUNT(*) FROM Orders;"), [[4]])
        self.s.run("BEGIN TRAN; BEGIN TRAN; UPDATE Orders SET Total = 1; COMMIT; COMMIT;")
        self.assertEqual(self.s.open_transactions, 0)
        self.assertEqual(self.rows("SELECT SUM(Total) FROM Orders;"), [[4]])
        self.s.run("BEGIN TRAN; UPDATE Orders SET Total = 9;")
        self.assertEqual(self.s.open_transactions, 1)

    def test_ddl(self):
        self.s.run("""CREATE TABLE Items (Sku CHAR(5) PRIMARY KEY, Qty SMALLINT NOT NULL CHECK (Qty >= 0), Price MONEY NULL);
                      CREATE UNIQUE NONCLUSTERED INDEX IX_Items_Price ON Items (Price DESC) INCLUDE (Qty) WHERE Price IS NOT NULL;
                      ALTER TABLE Items ADD Note NVARCHAR(100) NULL;
                      CREATE VIEW dbo.InStock AS SELECT Sku FROM Items WHERE Qty > 0;""")
        self.assertEqual(self.s.errors, [])
        self.assertEqual(self.s.columns("Items"), ["Sku", "Qty", "Price", "Note"])
        info = self.s.index_info("ix_items_price")
        self.assertEqual((info["columns"], info["includes"], info["unique"]), (["Price"], ["Qty"], True))
        self.assertTrue(self.s.has_view("InStock"))
        self.s.run("INSERT INTO Items (Sku, Qty) VALUES ('A', -1);")
        self.assertIn("CHECK constraint", self.s.errors[-1])
        self.s.run("INSERT INTO Items (Sku, Qty) VALUES ('B', 40000);")
        self.assertIn("overflow", self.s.errors[-1])
        self.s.run("INSERT INTO Items (Sku, Qty) VALUES ('C', 'lots');")
        self.assertTrue(self.s.errors[-1])
        self.s.run("DROP VIEW InStock; DROP TABLE IF EXISTS Items;")
        self.assertFalse(self.s.has_table("Items"))

    def test_identity_not_reused(self):
        self.s.run("DELETE FROM Customers WHERE CustomerId = 3; INSERT INTO Customers (Name) VALUES ('New');")
        self.assertEqual(self.rows("SELECT MAX(CustomerId) FROM Customers;"), [[4]])

    # ----- T-SQL rules SQLite does not enforce -----
    def test_group_by_rule(self):
        self.assertIn("not contained in either an aggregate function or the GROUP BY", self.error("SELECT Name, COUNT(*) FROM Customers;"))
        self.assertIn("'City'", self.error("SELECT Name, City, COUNT(*) FROM Customers GROUP BY Name;"))
        self.assertEqual(self.rows("SELECT UPPER(Name), COUNT(*) FROM Customers GROUP BY Name ORDER BY 1;")[0], ["ADA", 1])
        self.assertEqual(self.rows("SELECT YEAR(OrderDate) AS Y, COUNT(*) FROM Orders GROUP BY YEAR(OrderDate);"), [[2026, 4]])
        self.assertEqual(self.rows("SELECT COUNT(*), SUM(Total) FROM Orders;"), [[4, 42.75]])

    def test_alias_in_where_is_invalid(self):
        self.assertIn("Invalid column name 'Big'", self.error("SELECT Total * 2 AS Big FROM Orders WHERE Big > 10;"))
        self.assertEqual(self.rows("SELECT Total * 2 AS Big FROM Orders WHERE Total * 2 > 30 ORDER BY Big;"), [[40.0]])
        self.assertEqual(self.rows("SELECT Total * 2 AS Big FROM Orders ORDER BY Big DESC;")[0], [40.0])

    def test_order_by_in_subquery_needs_top(self):
        self.assertIn("ORDER BY clause is invalid", self.error("SELECT * FROM (SELECT Name FROM Customers ORDER BY Name) AS x;"))
        self.assertEqual(self.rows("SELECT * FROM (SELECT TOP 1 Name FROM Customers ORDER BY Name) AS x;"), [["Ada"]])

    def test_semicolon_before_cte(self):
        self.assertIn("syntax", self.error("SELECT 1 WITH x AS (SELECT 2) SELECT * FROM x;").lower())

    # ----- SQLite-isms and out-of-scope syntax are refused -----
    def test_sqlite_syntax_is_refused(self):
        for sql, fragment in [
            ("SELECT Name FROM Customers LIMIT 2;", "LIMIT is not T-SQL"),
            ("SELECT Name || City FROM Customers;", "'||' is not T-SQL"),
            ("SELECT * FROM Customers WHERE Active = TRUE;", "SQLite syntax"),
            ("SELECT Name FROM Customers WHERE Name GLOB 'A*';", "SQLite syntax"),
            ("PRAGMA table_info(Customers);", "Incorrect syntax"),
            ("SELECT IFNULL(City, 'x') FROM Customers;", "not a recognized built-in function"),
            ("SELECT randomblob(10);", "not a recognized built-in function"),
            ("SELECT * FROM sys.tables;", "System catalog"),
            ("SELECT * FROM sqlite_master;", "Invalid object name"),
            ("ATTACH DATABASE 'x.db' AS x;", "Incorrect syntax"),
        ]:
            with self.subTest(sql=sql):
                try:
                    self.s.run(sql)
                except t.TsqlError as error:
                    self.assertIn(fragment, str(error))
                else:
                    self.fail("expected a refusal: " + sql)

    def test_procedural_and_local_lab_features(self):
        for sql in ["IF 1 = 1 SELECT 1;", "WHILE 1 = 0 SELECT 1;", "EXEC sp_who;", "MERGE Customers AS c USING Orders AS o ON 1=1 WHEN MATCHED THEN DELETE;",
                    "UPDATE o SET Total = 1 FROM Orders o JOIN Customers c ON c.CustomerId = o.CustomerId;", "SELECT * INTO #x FROM Customers;"]:
            with self.subTest(sql=sql):
                with self.assertRaises(t.TsqlError):
                    self.s.run(sql)

    def test_runaway_query_is_stopped(self):
        self.s.run("WITH n(i) AS (SELECT 1 UNION ALL SELECT i + 1 FROM n) SELECT COUNT(*) FROM n;")
        self.assertIn("ran for too long", self.s.errors[-1])


class Helpers(unittest.TestCase):
    def test_same_rows(self):
        self.assertTrue(t.same_rows([[1, "a"], [2, "b"]], [[1, "a"], [2.0, "b"]]))
        self.assertFalse(t.same_rows([[1, "a"]], [[1, "A"]]))
        self.assertTrue(t.same_rows([[2], [1]], [[1], [2]], ordered=False))
        self.assertFalse(t.same_rows([[2], [1]], [[1], [2]], ordered=True))

    def test_tokenizer(self):
        kinds = [x.kind for x in t.tokenize("SELECT [a b], \"c\", N'x''y', @v, 1.5 /* a /* nested */ */ -- tail\n")]
        self.assertEqual(kinds, ["word", "br", "punct", "dq", "punct", "str", "punct", "var", "punct", "num"])
        with self.assertRaises(t.TsqlError):
            t.tokenize("SELECT 'open")


if __name__ == "__main__":
    unittest.main(verbosity=1)
