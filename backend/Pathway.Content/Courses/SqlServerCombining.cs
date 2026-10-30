namespace Pathway.Content;

static partial class Curriculum
{
    // Module 3 of the SQL Server course: answering questions that need more than one table or more
    // than one step. Joins, set operators, subqueries and common table expressions.
    private static SqlLessonSpec[] SqlServerCombiningSpecs() =>
    [
        new(
            "sqlserver-joins",
            "Combining data",
            "Joins: matching rows across tables",
            "Use INNER, LEFT, RIGHT and FULL joins and the ON condition to put related rows side by side.",
            "A join matches each row of one table with the rows of another where a condition (usually equal ids) is true, producing wider rows. An inner join keeps only matches. A left join also keeps every left row that has no match, filling the right-hand columns with NULL. Putting a test on the right table in WHERE instead of ON turns a left join back into an inner join.",
            [
                ("sqlserver-relational-model", "you know orders point at customers through a shared id, which is exactly what a join follows."),
                ("sqlserver-tables-keys", "you know primary keys identify a row and a FOREIGN KEY-style id column refers to one."),
                ("sqlserver-null-logic", "you know a missing match produces NULL and that comparisons with NULL are unknown."),
                ("sqlserver-select-where", "you can filter with WHERE and rename tables with aliases."),
            ],
            """
            ## The question a join answers
            Orders store a `CustomerId`, not a name. To print "Ada bought something for 10.50" you need the order *and* the customer's name, which live in two tables. A **join** builds new, wider rows by matching rows from the two tables.

            ```
            Customers                       Orders
            CustomerId | Name               OrderId | CustomerId | Total
                     1 | Ada                     10 |          1 | 10.50
                     2 | Grace                   11 |          1 | 20.00
                     3 | Linus                   12 |          2 |  5.25
            ```

            ## How an inner join works, step by step
            ```sql
            SELECT c.Name, o.OrderId, o.Total
            FROM Customers AS c
            INNER JOIN Orders AS o ON o.CustomerId = c.CustomerId;
            ```

            Picture the server taking each customer and looking for orders whose `CustomerId` equals it:
            1. Ada (1) matches orders 10 and 11, so two result rows: (Ada, 10, 10.50), (Ada, 11, 20.00).
            2. Grace (2) matches order 12: (Grace, 12, 5.25).
            3. Linus (3) matches nothing. An **inner join** keeps only rows that matched, so Linus does not appear.

            Parts of the syntax:
            - `c` and `o` are **aliases**. They keep the query short and, more importantly, tell the server which table a column comes from. If both tables had a `CustomerId` and you just wrote `CustomerId`, the server would refuse with "ambiguous column name".
            - `ON` holds the **join condition**: the rule for which row pairs belong together. It is nearly always "the foreign key equals the primary key". `INNER` is optional; plain `JOIN` means inner.

            The server does not literally loop like this. It picks a strategy with the cheapest cost (you will see them in the execution-plan lab), but the result is exactly what the step-by-step picture gives.

            ## Keeping the unmatched: outer joins
            What if you want a list of *every* customer, with their orders where they have any? Linus must stay. That is a **LEFT JOIN** (short for LEFT OUTER JOIN):

            ```sql
            SELECT c.Name, o.OrderId, o.Total
            FROM Customers AS c
            LEFT JOIN Orders AS o ON o.CustomerId = c.CustomerId;
            ```

            "Left" means the table written *before* the word JOIN, here `Customers`. Every left row is kept. When no right row matches, the right table's columns come back as NULL. The result adds one row: (Linus, NULL, NULL). The word NULL there means "no order exists", not "unknown".

            - **RIGHT JOIN** is the mirror image: keep every row from the table after JOIN. Most people just swap the table order and use LEFT, which reads more naturally.
            - **FULL JOIN** keeps unmatched rows from both sides.
            - **CROSS JOIN** pairs every row with every row (no condition). With 1,000 rows on each side that is 1,000,000 rows, so use it deliberately, for example to generate combinations.

            ## Finding what is missing
            A left join plus a test for NULL answers "which customers have never ordered?":

            ```sql
            SELECT c.Name
            FROM Customers AS c
            LEFT JOIN Orders AS o ON o.CustomerId = c.CustomerId
            WHERE o.OrderId IS NULL;
            ```

            Test a column that can never be NULL in a real match, such as the order's primary key, so the NULL can only mean "no match".

            ## The ON versus WHERE trap
            Suppose you want every customer, plus only their orders over 15. Many people write the order test in WHERE:

            ```sql
            ... LEFT JOIN Orders AS o ON o.CustomerId = c.CustomerId
            WHERE o.Total > 15     -- wrong for this goal
            ```

            For Linus, `o.Total` is NULL, so `NULL > 15` is unknown and WHERE drops him. Your left join has quietly become an inner join. Put the test on the right-hand table into the **ON** clause so it limits which orders match, while the left side still keeps all customers:

            ```sql
            ... LEFT JOIN Orders AS o ON o.CustomerId = c.CustomerId AND o.Total > 15
            ```

            Rule of thumb: conditions about *how rows match* go in ON; conditions about *which final rows to keep* go in WHERE. With an inner join the two give the same result, so only outer joins make you choose.

            ## One row can match many
            One customer with three orders produces three rows, and customer columns repeat on each. If you then SUM a customer-level number you will count it three times. When a result has more rows than you expect, look for a join that matches many. Joining three or more tables is the same idea repeated: add another `JOIN ... ON ...` for each table.

            ## Joining a table to itself
            A table can be joined to itself with two aliases, handy for hierarchies: an `Employees` table with a `ManagerId` can be joined as `e JOIN Employees AS m ON e.ManagerId = m.EmployeeId` to show each employee next to their manager's row.

            ## What the words mean
            - **Join:** building wider rows by matching rows from two tables.
            - **Join condition (ON):** the rule saying which row pairs match.
            - **Inner join:** keeps only matched rows.
            - **Left / right / full outer join:** also keeps unmatched rows from the left / right / both side(s), filling the missing columns with NULL.
            - **Cross join:** every row paired with every row.
            - **Foreign key:** a column holding the primary key of a row in another table.
            - **Ambiguous column:** a column name that exists in more than one joined table without an alias.

            ## Explain it to your team
            "A join lines up rows from two tables using a condition, usually foreign key equals primary key. An inner join keeps only matches; a left join also keeps every left row and shows NULLs where nothing matched, which is how we find customers with no orders. Matching rules go in ON, and a WHERE filter on the right table silently turns a left join into an inner join."
            """,
            """
            SELECT c.Name, o.OrderId, o.Total
            FROM Customers AS c
            LEFT JOIN Orders AS o ON o.CustomerId = c.CustomerId;
            """,
            SqlCode(
                "Every customer and their orders",
                "The tables are `Customers(CustomerId, Name)` and `Orders(OrderId, CustomerId, Total)`. Return `Name`, `OrderId` and `Total` for every customer paired with each of their orders. A customer who has no orders must still appear once, with NULL in `OrderId` and `Total`. The order of the rows does not matter.",
                [
                    "Return exactly the columns Name, OrderId and Total",
                    "Match orders to customers on CustomerId",
                    "Keep customers who have no orders (NULL in the order columns)",
                ],
                "-- Every customer with each of their orders (customers with none still appear).\nSELECT c.Name, o.OrderId, o.Total\nFROM Customers AS c\n",
                "You need a LEFT JOIN from Customers to Orders: `LEFT JOIN Orders AS o ON o.CustomerId = c.CustomerId`. An INNER JOIN would drop the customer with no orders.",
                ["returns Name, OrderId and Total", "keeps the customer who has no orders", "still right when the data changes"])
        ),
        new(
            "sqlserver-set-operators",
            "Combining data",
            "UNION, INTERSECT and EXCEPT: combining result sets",
            "Stack the results of two queries, or keep what they share, or subtract one from the other.",
            "Set operators combine the rows of two whole queries. UNION stacks them and removes repeats, UNION ALL stacks without removing repeats, INTERSECT keeps rows found in both, and EXCEPT keeps rows from the first that are missing from the second. Both queries must return the same number of columns with compatible types, and the column names come from the first query.",
            [
                ("sqlserver-select-where", "you can write a single SELECT with a WHERE."),
                ("sqlserver-sorting-paging", "you know ORDER BY goes last and applies to the whole combined result."),
                ("sqlserver-joins", "you can tell a join, which makes rows wider, from a set operator, which stacks rows on top of each other."),
            ],
            """
            ## Wider versus longer
            A join glues tables **side by side**: more columns per row. A **set operator** works **top to bottom**: it takes the rows from two queries and combines them into one list with the same columns. Use it when the same kind of thing is spread over two tables, such as employees and contractors, or this year's and last year's orders.

            ```
            Employees(City)         Contractors(City)
            Tulsa                   Boston
            Tulsa                   Denver
            Austin
            Boston
            ```

            ## The rules
            - Both queries must return the **same number of columns**.
            - Matching columns (first with first, second with second) must have **compatible types**, such as number with number or text with text.
            - The **names** of the result columns come from the first query.
            - `ORDER BY`, if used, goes once at the very end and applies to the combined result.

            ## UNION and UNION ALL
            ```sql
            SELECT City FROM Employees
            UNION
            SELECT City FROM Contractors;      -- Tulsa, Austin, Boston, Denver
            ```
            `UNION` stacks the rows and **removes duplicates**: Tulsa and Boston appear once. To find duplicates the server has to compare and sort, which costs time. `UNION ALL` stacks and keeps every row, repeats included, and is faster. Use `UNION ALL` when you know there are no repeats or you want them; use `UNION` when you need the list distinct.

            ## INTERSECT: what both have
            ```sql
            SELECT City FROM Employees
            INTERSECT
            SELECT City FROM Contractors;      -- Boston
            ```
            Only rows that appear in both results survive, and the output is distinct.

            ## EXCEPT: in the first, not in the second
            ```sql
            SELECT City FROM Employees
            EXCEPT
            SELECT City FROM Contractors;      -- Tulsa, Austin
            ```
            Order matters here: swapping the queries answers a different question (cities with contractors but no employees). The output is distinct. This is a tidy way to write "cities that have employees but no contractors" without a join or a subquery.

            ## Set operators and NULL
            Unlike `=`, set operators treat two NULLs as **the same** value, so a NULL in both queries counts as a match for INTERSECT and a repeat for UNION. That is a deliberate difference from the "unknown" logic you learned for comparisons.

            ## Order of evaluation
            When you chain several operators, `INTERSECT` is evaluated before `UNION` and `EXCEPT`, and the others go left to right. Use parentheses to say what you mean in mixed queries.

            ## Labelling where rows came from
            When you stack two sources, add a constant column to remember which is which:
            ```sql
            SELECT City, 'employee' AS Source FROM Employees
            UNION ALL
            SELECT City, 'contractor' FROM Contractors;
            ```

            ## A worked example
            Employees' cities are Tulsa, Austin, Boston; contractors' cities are Boston and Denver. Goal: employee cities with no contractor.
            1. The first query yields {Tulsa, Austin, Boston} (repeats removed by EXCEPT).
            2. The second yields {Boston, Denver}.
            3. EXCEPT removes from the first anything in the second: Boston goes. Result: {Tulsa, Austin}.
            4. Add `ORDER BY City` at the very end to sort alphabetically: Austin, Tulsa.

            ## What the words mean
            - **Set operator:** combines the rows of two queries into one result.
            - **UNION:** stacks and removes duplicates. **UNION ALL:** stacks and keeps duplicates.
            - **INTERSECT:** keeps rows present in both.
            - **EXCEPT:** keeps rows from the first query that are absent from the second.
            - **Compatible types:** types the server can place in the same column (for example both numbers).

            ## Explain it to your team
            "A join makes rows wider; a set operator stacks result sets with the same columns. UNION removes repeats, UNION ALL keeps them and is faster, INTERSECT keeps what both share, EXCEPT subtracts the second from the first. Column counts and types must match, names come from the first query, and ORDER BY goes once at the end."
            """,
            """
            SELECT City FROM Employees
            EXCEPT
            SELECT City FROM Contractors
            ORDER BY City;
            """,
            SqlCode(
                "Employee cities without contractors",
                "The tables are `Employees(EmployeeId, City)` and `Contractors(ContractorId, City)`. Return a single column `City` listing every city where the company has at least one employee but **no** contractors. Each city appears once, sorted A to Z.",
                [
                    "Return exactly one column named City",
                    "Use a set operator to subtract contractor cities from employee cities",
                    "Each city once, sorted alphabetically",
                ],
                "-- Cities with employees but no contractors, A to Z.\nSELECT City FROM Employees\n",
                "Put `EXCEPT` between `SELECT City FROM Employees` and `SELECT City FROM Contractors`, then `ORDER BY City` once at the end.",
                ["returns a single City column", "subtracts contractor cities and removes repeats", "still right when the data changes"])
        ),
        new(
            "sqlserver-subqueries",
            "Combining data",
            "Subqueries: a query inside a query",
            "Use a query's result as a value, a list or a temporary table inside another query, including ones that depend on the outer row.",
            "A subquery is a SELECT written inside another statement. A scalar subquery returns one value, an IN subquery returns a list, EXISTS asks whether any row is returned, and a derived table in FROM acts as a temporary table. A correlated subquery refers to the outer row and is conceptually run once for each outer row.",
            [
                ("sqlserver-select-where", "you know WHERE keeps rows whose condition is true."),
                ("sqlserver-aggregates-group-by", "you know AVG, MAX and COUNT, which most subqueries compute."),
                ("sqlserver-joins", "you can compare a join with a subquery, because many questions can be written either way."),
            ],
            """
            ## When one query is not enough
            Some questions have two steps: "products that cost more than the average" needs the average first, then the comparison. A **subquery** lets you do both in one statement by placing a SELECT, in parentheses, where a value or a table would go.

            ## Scalar subquery: one value
            If the inner query returns exactly one row with one column, you can use it like a number:

            ```sql
            SELECT Name, Price
            FROM Products
            WHERE Price > (SELECT AVG(Price) FROM Products);
            ```

            The server first computes the average once, then compares each product with it. If the inner query could return more than one row, SQL Server raises an error, so make sure it cannot (use an aggregate, or a unique key).

            ## IN subquery: a list
            When the inner query returns one column of many rows, `IN` tests membership:

            ```sql
            SELECT Name
            FROM Customers
            WHERE CustomerId IN (SELECT CustomerId FROM Orders WHERE Total > 100);
            ```
            "Customers who have at least one order over 100." Beware the opposite, `NOT IN`: if the inner list contains even one NULL, `NOT IN` returns **nothing** for any row, because `x <> NULL` is unknown (Lesson 5 again). Prefer `NOT EXISTS`, below.

            ## EXISTS: is there any row?
            `EXISTS (subquery)` is true if the subquery returns at least one row, and it does not care what the columns are:

            ```sql
            SELECT c.Name
            FROM Customers AS c
            WHERE NOT EXISTS (SELECT 1 FROM Orders AS o WHERE o.CustomerId = c.CustomerId);
            ```
            "Customers who never ordered." The inner query mentions `c.CustomerId`, a column of the *outer* query. That makes it a correlated subquery, which is the next idea. `NOT EXISTS` is safe with NULLs, which is why it beats `NOT IN`.

            ## Correlated subqueries
            A **correlated** subquery refers to a column of the outer row, so its answer can differ per row. Conceptually the server runs it once for each outer row (the optimizer is often cleverer and rewrites it as a join, but the *meaning* is "for each row"). Example: products that cost more than the average of **their own category**:

            ```sql
            SELECT p.Name, p.Category, p.Price
            FROM Products AS p
            WHERE p.Price > (SELECT AVG(q.Price)
                             FROM Products AS q
                             WHERE q.Category = p.Category);
            ```

            Read it as: "for each product p, compute the average price of the products q that share p's category, and keep p if it is above that." Two aliases, `p` and `q`, make it clear which copy of the table each column belongs to. The inner query cannot be run alone, because `p.Category` has no meaning without an outer row.

            ## Derived tables: a subquery in FROM
            A subquery in FROM acts as a temporary table, and it must be given an alias:

            ```sql
            SELECT t.CustomerId, t.Spent
            FROM (SELECT CustomerId, SUM(Total) AS Spent
                  FROM Orders
                  GROUP BY CustomerId) AS t
            WHERE t.Spent > 100;
            ```
            This lets you filter or join on a computed result. The next lesson shows a cleaner way to write exactly this.

            ## Subquery or join?
            Many questions can be written either way. Subqueries read closer to the English question ("customers with an order over 100"). Joins can bring columns from both tables into the result, which a subquery cannot. Modern optimizers usually produce the same plan for equivalent forms, so choose by clarity, and test performance with a real execution plan if it is slow.

            ## A worked example
            Products: Sencha 12 (Tea), Earl Grey 8 (Tea), Matcha 25 (Tea), Mug 10 (Gear), Kettle 40 (Gear). The Tea average is (12+8+25)/3 = 15; the Gear average is (10+40)/2 = 25.
            - Sencha 12 is not above 15. Earl Grey 8 is not. Matcha 25 is above 15, so it is kept.
            - Mug 10 is not above 25. Kettle 40 is above 25, so it is kept.
            Result: Matcha and Kettle. Using the overall average (19) instead would give the same here by luck; try changing a price to see the difference.

            ## What the words mean
            - **Subquery:** a SELECT nested inside another statement, in parentheses.
            - **Scalar subquery:** returns exactly one value.
            - **IN / NOT IN:** tests membership in a list of values.
            - **EXISTS / NOT EXISTS:** tests whether any row is returned.
            - **Correlated subquery:** refers to a column of the outer query, so it can give a different answer for each outer row.
            - **Derived table:** a subquery in FROM used as a temporary table; it needs an alias.

            ## Explain it to your team
            "A subquery is a query in parentheses used as a value, a list or a temporary table. A correlated subquery depends on the outer row, as in 'above their own category's average'. We use EXISTS instead of NOT IN because a single NULL in the list makes NOT IN return nothing. Joins and subqueries often express the same thing; we pick the clearer one and check the plan if it is slow."
            """,
            """
            SELECT p.Name, p.Category, p.Price
            FROM Products AS p
            WHERE p.Price > (SELECT AVG(q.Price) FROM Products AS q WHERE q.Category = p.Category);
            """,
            SqlCode(
                "Above their own category's average",
                "The table is `Products(ProductId, Name, Category, Price)`. Return `Name`, `Category` and `Price` of every product that costs **more than the average price of the products in its own category**. A product whose price equals the category average is not above it. The order of the rows does not matter.",
                [
                    "Return exactly the columns Name, Category and Price",
                    "Compare each product with the average of its own category (a correlated subquery)",
                    "Strictly greater than the average",
                ],
                "-- Products priced above the average of their own category.\nSELECT p.Name, p.Category, p.Price\nFROM Products AS p\n",
                "Use `WHERE p.Price > (SELECT AVG(q.Price) FROM Products AS q WHERE q.Category = p.Category)`; the inner `q.Category = p.Category` is what makes it correlated.",
                ["returns Name, Category and Price", "compares each product with its own category's average", "still right when the data changes"])
        ),
        new(
            "sqlserver-ctes",
            "Combining data",
            "Common table expressions: naming each step",
            "Break a hard query into named steps with WITH, and reuse a step more than once.",
            "A common table expression (CTE) is a named temporary result defined with WITH at the top of a statement and used like a table in the query below it. CTEs make multi-step queries readable, let you use one step several times, and, in their recursive form, walk hierarchies. A CTE is not stored: it exists only for that one statement.",
            [
                ("sqlserver-subqueries", "a CTE is mostly a cleaner way to write a derived table or repeated subquery."),
                ("sqlserver-aggregates-group-by", "the steps you will name are usually GROUP BY summaries."),
                ("sqlserver-joins", "you will join a CTE to a table exactly as you join two tables."),
            ],
            """
            ## The problem: nested queries are hard to read
            In the last lesson a derived table put a whole GROUP BY inside a FROM clause. Imagine three such steps nested inside each other and you can see the problem: you must read from the inside out. A **common table expression (CTE)** lets you write the steps **top to bottom, each with a name**.

            ## The shape
            ```sql
            WITH CustomerTotals (CustomerId, Spent) AS (
                SELECT CustomerId, SUM(Total)
                FROM Orders
                GROUP BY CustomerId
            )
            SELECT c.Name, t.Spent
            FROM CustomerTotals AS t
            JOIN Customers AS c ON c.CustomerId = t.CustomerId
            WHERE t.Spent > 100;
            ```

            - `WITH Name (columns) AS ( query )` defines the step. The column list in brackets is optional if the inner query already names its columns.
            - The main statement that follows can use `CustomerTotals` as if it were a table.
            - Nothing is stored. The CTE exists only for that one statement and vanishes afterwards. It is a *name for a query*, not a saved copy of the data, so the server may run its logic each time it is used.
            - If a statement in the same batch comes before the WITH, end the previous statement with a semicolon. SQL Server requires `;` right before `WITH`, which is one reason to end every statement with one.

            ## Several steps
            Separate steps with commas, and later steps can use earlier ones:
            ```sql
            WITH CustomerTotals AS (
                SELECT CustomerId, SUM(Total) AS Spent FROM Orders GROUP BY CustomerId
            ),
            AverageSpend AS (
                SELECT AVG(Spent) AS Average FROM CustomerTotals
            )
            SELECT t.CustomerId, t.Spent
            FROM CustomerTotals AS t
            CROSS JOIN AverageSpend AS a
            WHERE t.Spent > a.Average;
            ```
            Read it like a recipe: first work out each customer's total; then work out the average of those totals; then list customers above the average. Each step has a name that explains it.

            ## Reusing a step
            A CTE can be mentioned several times in the main query, which a derived table cannot do without repeating its text. In the exercise you will use `CustomerTotals` once to list customers and once inside a subquery that finds the average.

            ## Averages of averages
            Watch the meaning of "average" when you build on a CTE. The average of the per-customer totals counts each customer once, however many orders they made. The average of the order totals counts each order once. They are different numbers, and only the first answers "an average customer's spending".

            ## Recursive CTEs: walking a hierarchy
            A CTE may refer to itself, which lets SQL follow a tree of any depth, such as employees and managers:
            ```sql
            WITH Chain AS (
                SELECT EmployeeId, ManagerId, 1 AS Depth
                FROM Employees WHERE ManagerId IS NULL       -- the anchor: start at the top
                UNION ALL
                SELECT e.EmployeeId, e.ManagerId, c.Depth + 1
                FROM Employees AS e
                JOIN Chain AS c ON e.ManagerId = c.EmployeeId  -- the recursive step: add the next level
            )
            SELECT * FROM Chain;
            ```
            It has two parts joined by `UNION ALL`: an **anchor** that gives the starting rows, and a **recursive step** that finds the rows one level further, repeating until a round adds nothing. If your data has a loop (A manages B manages A) it would never end, so SQL Server stops after 100 rounds by default with an error; `OPTION (MAXRECURSION n)` changes that limit. This course's built-in checker allows recursion but caps it, so deep trees are best tried on your lab server.

            ## CTE or temporary table?
            A CTE is just a name for a query. If you need to reuse an expensive result across several statements, or want the server to hold actual data with statistics, a **temporary table** (`#Name`) stores real rows. The performance module compares them.

            ## A worked example
            Customers: Ada's orders total 10 + 20 = 30; Grace 5; Linus 60; Idle has none. Goal: customers above the average customer total.
            1. `CustomerTotals` gives (Ada 30), (Grace 5), (Linus 60). Idle has no orders, so no row.
            2. The average of those totals is (30 + 5 + 60) / 3, about 31.7. Idle is not counted.
            3. Customers above that: Linus. Ada's 30 is just below.

            ## What the words mean
            - **CTE (common table expression):** a named temporary result defined with WITH for one statement.
            - **Anchor / recursive step:** the starting rows / the part that repeatedly finds the next level of a recursive CTE.
            - **Recursion:** a definition that refers to itself.
            - **Derived table:** a subquery in FROM; a CTE is a more readable way to write one.
            - **Temporary table:** real rows stored for the session, written with a leading #.

            ## Explain it to your team
            "A CTE gives a query step a name using WITH, so complex questions read top to bottom like a recipe. It exists only for that statement and is not stored data. We can reuse a step, chain several, and use the recursive form with an anchor and a recursive step to walk a hierarchy, while watching the recursion limit."
            """,
            """
            WITH CustomerTotals (CustomerId, Spent) AS (
                SELECT CustomerId, SUM(Total) FROM Orders GROUP BY CustomerId
            )
            SELECT c.Name, t.Spent
            FROM CustomerTotals AS t
            JOIN Customers AS c ON c.CustomerId = t.CustomerId
            WHERE t.Spent > (SELECT AVG(Spent) FROM CustomerTotals)
            ORDER BY t.Spent DESC;
            """,
            SqlCode(
                "Above-average customers",
                "The tables are `Customers(CustomerId, Name)` and `Orders(OrderId, CustomerId, Total)`. Use a CTE named `CustomerTotals` with columns `CustomerId` and `Spent` (the sum of each customer's order totals). Then return `Name` and `Spent` for every customer whose `Spent` is **above the average `Spent` across customers who have orders**. Customers with no orders are not part of the average and do not appear. Biggest spender first.",
                [
                    "Define a CTE named CustomerTotals with the columns CustomerId and Spent",
                    "Return exactly the columns Name and Spent",
                    "Keep customers whose Spent is above the average of CustomerTotals",
                    "Sort with the biggest Spent first",
                ],
                "-- Customers who spent more than the average customer, using a CTE.\nWITH CustomerTotals (CustomerId, Spent) AS (\n    SELECT CustomerId, SUM(Total) FROM Orders GROUP BY CustomerId\n)\nSELECT\n",
                "Join `CustomerTotals` to `Customers`, and compare with `(SELECT AVG(Spent) FROM CustomerTotals)`. Because the CTE has one row per customer, that is the average per customer, not per order.",
                ["uses a CTE called CustomerTotals", "compares against the average of customers with orders", "still right when the data changes"])
        ),
    ];
}
