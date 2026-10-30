namespace Pathway.Content;

static partial class Curriculum
{
    // Module 2 of the SQL Server course: reading data from one table. SELECT and WHERE, the trap of
    // NULL, sorting and paging, building values with expressions, and summarising with aggregates.
    private static SqlLessonSpec[] SqlServerQueryingSpecs() =>
    [
        new(
            "sqlserver-select-where",
            "Querying",
            "SELECT and WHERE: asking a table a question",
            "Choose columns with SELECT, name the table with FROM, and keep only matching rows with WHERE.",
            "A SELECT statement names the columns you want, the table they come from, and a WHERE condition that keeps only the rows for which the condition is true. Conditions are built from comparisons joined by AND, OR and NOT, plus the shortcuts IN, BETWEEN and LIKE.",
            [
                ("sqlserver-relational-model", "you know a table is rows and columns and that SQL says what result you want rather than how to find it."),
                ("sqlserver-tables-keys", "you know the data types and that Price-style numbers, text and dates behave differently."),
            ],
            """
            ## A query is a question with three parts
            Most of what you will ever do with a database is read it. You do that with a `SELECT` statement. Think of it as three questions answered in a fixed order:

            1. **FROM:** which table do I look in?
            2. **WHERE:** which rows do I keep?
            3. **SELECT:** which columns do I show?

            ```sql
            SELECT Name, Price
            FROM Products
            WHERE Category = 'Tea';
            ```

            You write `SELECT` first, but the server *thinks* in the order FROM, then WHERE, then SELECT. This matters later: a nickname you give a column in the SELECT line does not exist yet when WHERE runs, so you cannot use it in WHERE.

            Here is the table used in this lesson:

            ```
            Products
            ProductId | Name      | Category | Price
            ----------+-----------+----------+------
                    1 | Sencha    | Tea      | 12.50
                    2 | Earl Grey | Tea      |  9.99
                    3 | Espresso  | Coffee   | 14.00
                    4 | Matcha    | Tea      | 25.00
                    5 | Mug       | Gear     | 10.00
            ```

            ## Choosing columns
            - `SELECT Name, Price` returns just those two columns, in that order.
            - `SELECT *` returns every column. It is fine for a quick look, but avoid it in code other people depend on: if someone adds a column, your results silently change.
            - `SELECT DISTINCT Category` removes repeated rows from the result, so you get Tea, Coffee and Gear once each.
            - **Aliases** rename a column in the result: `SELECT Price AS Cost`. The same works for tables: `FROM Products AS p`. Aliases only change the name in the result; the table is untouched.

            ## WHERE: keeping rows
            WHERE holds a **condition**. The server tests it against each row and keeps the row only if the condition is *true*. The building blocks:

            - Comparisons: `=` (equal; note it is a single `=`), `<>` (not equal), `<`, `<=`, `>`, `>=`.
            - Text goes in single quotes, numbers do not: `Category = 'Tea'`, `Price >= 10`.
            - **AND** requires both sides true, **OR** requires either, **NOT** flips a condition. Use parentheses whenever you mix AND and OR. AND is evaluated before OR, so `A OR B AND C` means `A OR (B AND C)`, which is often not what you meant.
            - **IN** checks a list: `Category IN ('Tea', 'Coffee')` is the same as `Category = 'Tea' OR Category = 'Coffee'`, but easier to read.
            - **BETWEEN** checks a range, ends included: `Price BETWEEN 10 AND 20` means `Price >= 10 AND Price <= 20`. Both ends count, so it is easy to be off by one on dates (see below).
            - **LIKE** matches text patterns. `%` stands for any run of characters (including none) and `_` for exactly one character: `Name LIKE 'S%'` finds names starting with S, `Name LIKE '%a'` names ending in a, `Name LIKE '_ug'` finds Mug and Bug.

            ## Case and trailing spaces
            A fresh SQL Server database normally uses a **collation** (a rulebook for comparing text) that ignores upper and lower case, so `Category = 'tea'` matches the value Tea. A database can be created with a different collation, and that changes this behaviour, so never assume it on someone else's server. Also, SQL Server ignores trailing spaces when comparing with `=`, so `'Tea'` equals `'Tea   '`.

            ## Dates in a condition
            Write dates as text in the unambiguous year-month-day form: `OrderDate >= '2026-03-01'`. Avoid `'03/04/2026'`, which means March 4 or April 3 depending on language settings. With a column that holds both date and time, `BETWEEN '2026-03-01' AND '2026-03-31'` stops at midnight at the start of March 31, so it loses most of that last day. The safe pattern is "greater than or equal to the start, and strictly less than the day after the end": `OrderDate >= '2026-03-01' AND OrderDate < '2026-04-01'`.

            ## Reading your own query
            A good habit: say your WHERE out loud in plain words and check it matches the question. "Tea products that cost at least 10" becomes `Category = 'Tea' AND Price >= 10`. If you hear yourself say "or", check it is not secretly "and". Words like "and" in English often become OR in SQL: "customers in London and Paris" means a row is kept if its city is London OR Paris, because no single row is in two cities.

            ## A worked example
            Question: names of products that are tea and cost at least 10, or any coffee. Step by step:
            1. The tea part is `Category = 'Tea' AND Price >= 10`.
            2. The coffee part is `Category = 'Coffee'`.
            3. Either part qualifies, so join them with OR, and put the first part in parentheses to be clear:
            ```sql
            SELECT Name
            FROM Products
            WHERE (Category = 'Tea' AND Price >= 10) OR Category = 'Coffee';
            ```
            On the table above this returns Sencha, Espresso and Matcha. Earl Grey is tea but costs under 10; the Mug is neither tea nor coffee.

            ## What the words mean
            - **SELECT:** the clause that lists the columns to show.
            - **FROM:** the clause that names the table to read.
            - **WHERE:** the clause holding the condition rows must satisfy.
            - **Condition (predicate):** a test that is true or false (or unknown, see the next lesson) for each row.
            - **Alias:** a temporary nickname for a column or table, introduced with AS.
            - **DISTINCT:** removes repeated rows from the result.
            - **Wildcard:** `%` (any run of characters) or `_` (one character) inside a LIKE pattern.
            - **Collation:** the rulebook for comparing and sorting text, including whether case matters.

            ## Explain it to your team
            "A SELECT reads like a question: FROM says which table, WHERE says which rows, SELECT says which columns. WHERE keeps a row only when its condition is true. Combine tests with AND, OR and parentheses, and use IN, BETWEEN and LIKE for lists, ranges and patterns. For dates, use year-month-day text and a half-open range so we never lose the last day."
            """,
            """
            SELECT Name, Price
            FROM Products
            WHERE Category = 'Tea' AND Price >= 10;
            """,
            SqlCode(
                "Expensive teas",
                "The table is `Products(ProductId, Name, Category, Price)`; the example rows are shown in the lesson. Write one query that returns the `Name` and `Price` of every product in the `Tea` category that costs 10 or more (10 itself counts). The order of the rows does not matter. The grader also runs your query on different data to make sure you did not just copy the answer.",
                [
                    "Return exactly the columns Name and Price, in that order",
                    "Keep only products whose Category is Tea",
                    "Keep only products whose Price is at least 10",
                ],
                "-- Return Name and Price of Tea products costing 10 or more.\nSELECT\n",
                "Use `WHERE Category = 'Tea' AND Price >= 10`. A price of exactly 10 must be kept, so you need `>=`, not `>`.",
                ["returns Name and Price", "keeps only Tea at 10 or more", "still right when the data changes"])
        ),
        new(
            "sqlserver-null-logic",
            "Querying",
            "NULL and three-valued logic",
            "Why a comparison with a missing value is neither true nor false, and how to write conditions that handle it on purpose.",
            "NULL means a value is unknown. Any comparison with an unknown value gives UNKNOWN, not true or false, and WHERE keeps only rows that are true. So a row with a NULL city is dropped by both City = 'London' and City <> 'London'. Test for missing values with IS NULL and IS NOT NULL, and decide explicitly what NULL should do in every condition.",
            [
                ("sqlserver-tables-keys", "you know NULL is a missing value and which columns allow it."),
                ("sqlserver-select-where", "you can write a WHERE condition and combine tests with AND, OR and NOT."),
            ],
            """
            ## A surprise
            Take this table:

            ```
            Customers
            CustomerId | Name   | City     | Active
            -----------+--------+----------+-------
                     1 | Ada    | London   | 1
                     2 | Grace  | New York | 1
                     3 | Linus  | (NULL)   | 1
            ```

            Ask for customers *not* in London:

            ```sql
            SELECT Name FROM Customers WHERE City <> 'London';
            ```

            You expect Grace and Linus. You get only Grace. Linus has vanished, and no error was shown. This is the single most common source of silently wrong reports in SQL, so we will take it slowly.

            ## What NULL means
            `NULL` means **"unknown"** or "no value recorded". Linus's city is not "nothing", it is "we don't know". Now ask: is Linus's city different from London? You cannot tell. He might live in London; he might not. The honest answer to the question "is an unknown thing different from London?" is "unknown".

            ## Three truth values
            Ordinary logic has two answers, true and false. SQL has **three**: true, false and **unknown**. Any comparison that touches NULL, including `NULL = NULL` and `NULL <> 'London'`, gives unknown. Even "is this unknown equal to that unknown?" is unknown, because they might be different things.

            The rule that causes the surprise is simple: **WHERE keeps a row only if its condition is true.** Unknown is not true, so the row is dropped. That is why `City = 'London'` and `City <> 'London'` each drop Linus. Between them, they do not cover everyone.

            ## How AND, OR and NOT treat unknown
            It helps to think of unknown as "could be either" and ask if the answer is still certain:

            - `TRUE AND UNKNOWN` is unknown (it depends on the unknown part). `FALSE AND UNKNOWN` is false, since one false side already decides it.
            - `TRUE OR UNKNOWN` is true, since one true side already decides it. `FALSE OR UNKNOWN` is unknown.
            - `NOT UNKNOWN` is unknown. Flipping something you don't know leaves you not knowing.

            ## Testing for NULL
            Because `= NULL` can never be true, SQL gives you dedicated tests:

            ```sql
            WHERE City IS NULL        -- the city is missing
            WHERE City IS NOT NULL    -- the city is present
            ```

            Writing `WHERE City = NULL` is a classic mistake: it is unknown for every row, so it returns nothing.

            ## Saying what you mean
            When you write a condition, decide what should happen to rows with a missing value, then write it explicitly. For "customers not in London, including those whose city we don't know":

            ```sql
            SELECT Name FROM Customers WHERE City <> 'London' OR City IS NULL;
            ```

            For "customers whose city we know and it's not London", the original condition was already right, but now you chose it on purpose.

            ## Handy NULL tools
            - **COALESCE(a, b, c)** returns the first value that is not NULL. `COALESCE(City, 'Unknown')` shows "Unknown" in place of a missing city. Use it for display, not to hide a data problem.
            - **ISNULL(a, b)** is the SQL Server version with exactly two arguments. COALESCE is standard SQL and handles several values; prefer it unless you have a reason.
            - **NULLIF(a, b)** returns NULL when a equals b, otherwise a. It is the clean way to avoid dividing by zero: `Total / NULLIF(Count, 0)` gives NULL instead of an error when Count is 0.

            ## NULL elsewhere
            - Arithmetic with NULL gives NULL: `5 + NULL` is NULL. String joining behaves the same in the default settings, so `'Hi ' + NULL` is NULL (a trap in names and addresses; COALESCE each piece).
            - Aggregates such as SUM and AVG skip NULLs; `COUNT(*)` counts rows while `COUNT(City)` counts only rows where City is not NULL. You will meet this again in the aggregates lesson.
            - In a **sort**, SQL Server treats NULL as smaller than any value, so NULLs come first in ascending order.
            - For uniqueness, SQL Server's UNIQUE constraint allows only one NULL in the column.

            ## What the words mean
            - **NULL:** a marker meaning the value is unknown or missing.
            - **Three-valued logic:** logic with true, false and unknown.
            - **Unknown:** the result of comparing something to NULL; WHERE treats it like false.
            - **IS NULL / IS NOT NULL:** the only correct tests for a missing or present value.
            - **COALESCE:** returns the first argument that is not NULL.
            - **NULLIF:** returns NULL if its two arguments are equal.

            ## Explain it to your team
            "NULL means unknown, so any comparison with it is unknown, and WHERE only keeps rows where the condition is true. That is why City <> 'London' silently drops customers with no city. We test missing values with IS NULL, and for every condition we decide on purpose what should happen to rows with unknowns, using COALESCE or an explicit OR IS NULL."
            """,
            """
            -- Linus (City is NULL) is dropped by this:
            SELECT Name FROM Customers WHERE City <> 'London';

            -- Say what you mean:
            SELECT Name FROM Customers WHERE City <> 'London' OR City IS NULL;
            """,
            SqlCode(
                "Not in London, on purpose",
                "The table is `Customers(CustomerId, Name, City, Active)`; `City` may be NULL and `Active` is 1 or 0. Write a query that returns `Name` and `City` of every active customer (Active = 1) who is not known to live in London. A customer whose city is NULL counts as not in London and must be included. Show the newest customer first, meaning the highest `CustomerId` first.",
                [
                    "Return exactly the columns Name and City",
                    "Only customers with Active = 1",
                    "Exclude London, but keep customers whose City is NULL",
                    "Highest CustomerId first",
                ],
                "-- Active customers who are not in London (NULL city included), newest first.\nSELECT Name, City\nFROM Customers\n",
                "Parenthesise the OR: `WHERE Active = 1 AND (City <> 'London' OR City IS NULL)`. Without the brackets AND binds tighter and the active test is skipped for NULL cities.",
                ["returns Name and City", "filters on Active and City", "keeps the customer whose City is NULL, newest first"])
        ),
        new(
            "sqlserver-sorting-paging",
            "Querying",
            "ORDER BY, TOP and paging",
            "Put rows in a defined order, take the first few, and fetch one page at a time.",
            "A table has no built-in row order, so a result is only in a reliable order if you add ORDER BY. TOP n takes the first n rows after ordering, and OFFSET ... FETCH skips rows and then takes a page, which is how lists in applications are split into pages. Break ties with a second sort column so pages are stable.",
            [
                ("sqlserver-select-where", "you can write SELECT ... FROM ... WHERE and choose columns."),
                ("sqlserver-null-logic", "you know how NULL behaves, which decides where missing values land in a sort."),
            ],
            """
            ## Rows have no order unless you ask
            A table is a *set* of rows, like a pile of index cards. The pile has no first or last card. If you run `SELECT Name FROM Products` today and again tomorrow, the server may return the rows in a different order, for example because an index was added or the data moved. Code that "happens to work" because rows usually arrive in id order will break one day. **If the order matters, say so with ORDER BY.** It is always the last clause of a query.

            ```sql
            SELECT Name, Price
            FROM Products
            ORDER BY Price DESC;
            ```

            - `ASC` is smallest first (the default); `DESC` is largest first.
            - Text sorts by the collation's alphabetical rules; dates sort by time; NULL counts as the smallest value, so it comes first in `ASC` and last in `DESC`.
            - You may sort by several columns: `ORDER BY Category, Price DESC` sorts by category first, and within each category by price from high to low. Each column gets its own `ASC` or `DESC`.
            - You may sort by an alias from SELECT (`ORDER BY Cost`), because ORDER BY runs *after* SELECT, unlike WHERE.

            ## Ties and stable results
            If two rows have the same value in your sort column, the server may return them in either order, and it may be a different order the next time. When you paginate (below) this is a real bug: the same product could appear at the end of page 1 and again at the start of page 2, or never appear at all. The fix is to always end ORDER BY with a column that makes every row unique, usually the primary key: `ORDER BY Price DESC, ProductId`.

            ## TOP: just the first few
            `TOP` returns only the first rows of the sorted result:

            ```sql
            SELECT TOP (3) Name, Price
            FROM Products
            ORDER BY Price DESC;      -- the three most expensive
            ```

            Without ORDER BY, TOP gives you "any three", which is rarely what you want. Two useful extras: `TOP (10) PERCENT` takes a share of the rows, and `TOP (3) WITH TIES` also returns any further rows that tie with the last row, so you may get more than 3.

            ## OFFSET and FETCH: pages
            An application shows a list in pages: page 1 is rows 1 to 20, page 2 is rows 21 to 40, and so on. T-SQL does this with `OFFSET` (how many rows to skip) and `FETCH` (how many to take):

            ```sql
            SELECT Name, Price
            FROM Products
            ORDER BY Price DESC, Name
            OFFSET 20 ROWS
            FETCH NEXT 20 ROWS ONLY;     -- page 2 of size 20
            ```

            The arithmetic: to show page `p` (counting from 1) with `s` rows per page, skip `(p - 1) × s` rows and fetch `s`. For page 3 of size 20 you skip 40. OFFSET/FETCH requires an ORDER BY, because "skip 20" means nothing without an order.

            ## A caution on deep pages
            Skipping rows is not free. The server still produces and discards the first 100,000 rows to reach page 5,001. For huge tables there is a faster idea called keyset paging ("give me the 20 rows after the last one I saw"). It is mentioned again in the performance module; for now, OFFSET/FETCH is correct and clear.

            ## A worked example
            Products sorted by price descending (ties by name): B 9, E 8, C 7, F 6, A 5, G 4, D 3, H 2. With a page size of 3, page 1 is B, E, C. Page 2 skips 3 rows and takes the next 3: F, A, G.

            ## What the words mean
            - **ORDER BY:** the final clause that sorts the result.
            - **ASC / DESC:** ascending (smallest first) / descending (largest first).
            - **Tie:** two rows with equal values in the sort column.
            - **TOP (n):** keep only the first n rows of the result.
            - **OFFSET n ROWS:** skip the first n rows of the sorted result.
            - **FETCH NEXT n ROWS ONLY:** then return the next n rows.
            - **Pagination:** splitting a long list into pages.

            ## Explain it to your team
            "Rows have no natural order, so any list that must be in order needs ORDER BY, and it should end with a unique column so ties cannot shuffle. TOP gives the first few rows; OFFSET and FETCH give a page by skipping (page minus one) times page size. Without a stable order, pages can repeat or lose rows."
            """,
            """
            SELECT Name, Price
            FROM Products
            ORDER BY Price DESC, Name
            OFFSET 3 ROWS
            FETCH NEXT 3 ROWS ONLY;     -- page 2 when each page has 3 rows
            """,
            SqlCode(
                "Page two of the price list",
                "The table is `Products(ProductId, Name, Price)`. Return the `Name` and `Price` of the products on **page 2** when the list is shown with **3 products per page**, most expensive first. If prices are equal, the product whose name comes first alphabetically goes first, so the pages never shuffle.",
                [
                    "Return exactly the columns Name and Price",
                    "Sort by Price from highest to lowest, then by Name",
                    "Use OFFSET and FETCH to return only page 2 (rows 4 to 6)",
                ],
                "-- Page 2 of the products, 3 per page, most expensive first.\nSELECT Name, Price\nFROM Products\n",
                "Sort with `ORDER BY Price DESC, Name`, then add `OFFSET 3 ROWS FETCH NEXT 3 ROWS ONLY`. You skip (2 - 1) x 3 = 3 rows.",
                ["returns Name and Price", "skips the first page and returns three rows in price order", "breaks ties by Name"])
        ),
        new(
            "sqlserver-expressions-functions",
            "Querying",
            "Expressions, functions and CASE",
            "Build new values from columns with arithmetic, text and date functions, conversions and CASE.",
            "The SELECT list can hold expressions, not just column names: arithmetic, joined text, built-in functions and CASE for if-then-else. Each expression produces a new column computed for every row. Watch for NULL in any of them, convert types deliberately with CAST, and give every computed column an alias.",
            [
                ("sqlserver-select-where", "you can write a SELECT list and give a column an alias with AS."),
                ("sqlserver-null-logic", "you know any expression touching NULL gives NULL, and how COALESCE repairs that."),
                ("sqlserver-tables-keys", "you know the data types, because functions and conversions depend on them."),
            ],
            """
            ## Columns can be computed
            So far SELECT has listed existing columns. It can also compute new values for every row. A computed value is called an **expression**. This lets the database do the work of formatting and deriving data instead of every application repeating it.

            ```sql
            SELECT Name, Price, Price * 1.08 AS PriceWithTax
            FROM Products;
            ```

            `PriceWithTax` does not exist in the table. The server calculates it for each row on the way out. Always give a computed column an alias; otherwise the column has no name, which tools and code find awkward.

            ## Arithmetic
            `+`, `-`, `*`, `/` and `%` (remainder) work on numbers. Two traps:
            - **Integer division:** when both sides are whole numbers the answer is a whole number. `7 / 2` is `3`, not 3.5. Make one side a decimal: `7 / 2.0` is 3.5.
            - **Division by zero** is an error in SQL Server. Guard it with NULLIF: `Total / NULLIF(Qty, 0)`.

            ## Text
            - **Joining** uses `+`: `FirstName + ' ' + LastName`. If either part is NULL the whole thing is NULL; wrap each part in COALESCE. The function `CONCAT(a, b, c)` instead treats NULL as empty text.
            - `UPPER(x)`, `LOWER(x)` change case. `LEN(x)` counts characters (ignoring trailing spaces). `TRIM(x)` removes spaces at both ends.
            - `SUBSTRING(x, start, length)` takes a piece; positions start at **1**, not 0. `LEFT(x, n)` and `RIGHT(x, n)` take from the ends.
            - `REPLACE(x, old, new)` swaps text. `CHARINDEX(find, x)` returns the position where `find` first appears, or 0 if it is absent.

            ## Dates
            - `GETDATE()` is the current date and time on the server.
            - `DATEADD(part, number, date)` adds time: `DATEADD(day, 7, OrderDate)` is a week later. `part` can be `year`, `month`, `day`, `hour` and so on.
            - `DATEDIFF(part, start, end)` counts how many **boundaries** of that part are crossed between two dates. `DATEDIFF(day, '2026-03-01', '2026-03-04')` is 3. Because it counts boundaries, not full periods, `DATEDIFF(year, '2025-12-31', '2026-01-01')` is 1, though only one day passed.
            - `YEAR(d)`, `MONTH(d)`, `DAY(d)` pull out a part. `FORMAT(d, 'yyyy-MM-dd')` formats a date as text, but it is slow on large tables, so prefer the other functions for filtering.

            ## Converting types
            `CAST(expression AS type)` converts a value: `CAST('42' AS INT)` gives the number 42, and `CAST(Price AS INT)` drops the fraction. If the conversion is impossible, such as `CAST('abc' AS INT)`, the statement fails with an error. `TRY_CAST` returns NULL instead of failing, which is useful for dirty data. SQL Server also converts quietly when you mix types (a text compared to a number); that **implicit conversion** can slow queries and surprise you, so make conversions visible.

            ## CASE: if, then, else
            `CASE` picks a value depending on conditions. It reads top to bottom and returns the result of the first condition that is true:

            ```sql
            SELECT Name,
                   CASE WHEN Price < 5  THEN 'Budget'
                        WHEN Price < 20 THEN 'Standard'
                        ELSE 'Premium'
                   END AS PriceBand
            FROM Products;
            ```

            Because it stops at the first match, order matters: a price of 3 is both "under 5" and "under 20", and it gets Budget because that test is first. Without `ELSE`, a row that matches nothing gets NULL.

            ## A worked example
            Goal: a label such as `SENCHA - Tea` for each product, with `Unsorted` when there is no category.
            1. Make the name upper case: `UPPER(Name)`.
            2. A missing category must not turn the whole label NULL, so replace it first: `COALESCE(Category, 'Unsorted')`.
            3. Join the pieces with `+`: `UPPER(Name) + ' - ' + COALESCE(Category, 'Unsorted')`.
            4. Name the column: `AS Label`.

            ## Where expressions can go
            Expressions work in SELECT, WHERE and ORDER BY. A computed column's alias can be used in ORDER BY but not in WHERE, for the reason from Lesson 4: WHERE runs before SELECT. Also, wrapping a column in a function inside WHERE (for example `WHERE YEAR(OrderDate) = 2026`) can stop the server from using an index on that column; a range test avoids that, as you will see in the performance module.

            ## What the words mean
            - **Expression:** anything that computes a value: a column, a literal, arithmetic, a function call or a CASE.
            - **Function:** a named, built-in calculation that takes values and returns a value.
            - **Literal:** a value written directly in the query, like `'Tea'` or `10`.
            - **CAST:** converts a value from one data type to another.
            - **Implicit conversion:** a conversion the server performs on its own when types differ.
            - **CASE:** a built-in if-then-else expression.

            ## Explain it to your team
            "SELECT can compute new columns with arithmetic, text and date functions, and CASE for if-then-else. We alias every computed column. NULL turns arithmetic and + joins into NULL, so we COALESCE; integer division drops fractions, so we make one side decimal; and we convert types with CAST on purpose rather than letting the server guess."
            """,
            """
            SELECT Name,
                   UPPER(Name) + ' - ' + COALESCE(Category, 'Unsorted') AS Label,
                   CASE WHEN Price < 5 THEN 'Budget'
                        WHEN Price < 20 THEN 'Standard'
                        ELSE 'Premium' END AS PriceBand
            FROM Products;
            """,
            SqlCode(
                "Label and price band",
                "The table is `Products(ProductId, Name, Category, Price)`; `Category` may be NULL. Return three columns: `Name`; `Label`, which is the product's name in upper case, then ` - ` (a space, a dash, a space), then its category, or the word `Unsorted` when the category is NULL; and `PriceBand`, which is `Budget` when the price is under 5, `Standard` when it is 5 or more but under 20, and `Premium` for 20 or more.",
                [
                    "Return exactly the columns Name, Label and PriceBand",
                    "Label is UPPER(Name), then ' - ', then Category (or 'Unsorted' if Category is NULL)",
                    "PriceBand: under 5 is Budget, 5 to under 20 is Standard, 20 or more is Premium",
                ],
                "-- Name, Label and PriceBand for every product.\nSELECT Name\nFROM Products;\n",
                "Use `UPPER(Name) + ' - ' + COALESCE(Category, 'Unsorted') AS Label` and a CASE with `WHEN Price < 5`, then `WHEN Price < 20`, then ELSE.",
                ["returns Name, Label and PriceBand", "builds the label (upper case, NULL category replaced) and the band", "band edges are right"])
        ),
        new(
            "sqlserver-aggregates-group-by",
            "Querying",
            "Aggregates, GROUP BY and HAVING",
            "Summarise many rows into one: counts, totals and averages, per group, and filter the groups.",
            "An aggregate function (COUNT, SUM, AVG, MIN, MAX) collapses many rows into one value. GROUP BY splits the rows into groups first, so you get one summary row per group. WHERE filters rows before they are grouped; HAVING filters the groups after. Every column in SELECT must be either grouped or inside an aggregate.",
            [
                ("sqlserver-select-where", "you can filter rows with WHERE, which runs before grouping."),
                ("sqlserver-null-logic", "you know aggregates skip NULL, and COUNT(*) differs from COUNT(column)."),
                ("sqlserver-sorting-paging", "you can sort the summary rows with ORDER BY."),
                ("sqlserver-expressions-functions", "you can name a computed column with an alias, which summaries need."),
            ],
            """
            ## From rows to a summary
            A report rarely lists every row. It asks "how many orders?", "how much did each customer spend?", "what is the average price per category?". Questions like these are answered with **aggregate functions**, which take many rows and return one value:

            | Function | Meaning |
            | --- | --- |
            | `COUNT(*)` | how many rows |
            | `COUNT(col)` | how many rows where `col` is not NULL |
            | `SUM(col)` | the total of `col` |
            | `AVG(col)` | the average of `col` |
            | `MIN(col)`, `MAX(col)` | smallest, largest |

            ```sql
            SELECT COUNT(*) AS OrderCount, SUM(Total) AS Revenue, AVG(Total) AS AverageOrder
            FROM Orders;
            ```

            With no grouping, the whole table is one group, so you get one row back. All of these skip NULL values, except `COUNT(*)`, which counts rows. Two facts to remember: `AVG` of an empty set (no rows) is NULL, and `AVG` of a **whole-number** column such as INT returns a whole number in SQL Server, so averaging 1 and 2 gives 1, not 1.5. Cast first when you want the fraction: `AVG(CAST(Qty AS DECIMAL(10,2)))`.

            ## GROUP BY: one summary per group
            Add `GROUP BY` to split the rows into groups before summarising. Rows with the same value in the grouped column fall into the same group, and you get one result row per group:

            ```sql
            SELECT CustomerId, COUNT(*) AS OrderCount, SUM(Total) AS TotalSpent
            FROM Orders
            GROUP BY CustomerId;
            ```

            Think of sorting the orders into piles, one pile per customer, then counting and adding up each pile.

            **The one rule:** every column in the SELECT list must be either in the GROUP BY or inside an aggregate function. If you wrote `SELECT CustomerId, OrderDate ... GROUP BY CustomerId`, the pile for customer 1 holds many dates, so which one should be shown? SQL Server refuses to guess and raises an error. Either group by the date too, or aggregate it (`MAX(OrderDate)` for the latest).

            You can group by several columns (`GROUP BY CustomerId, YEAR(OrderDate)`), giving a pile for each combination, and by expressions.

            ## WHERE versus HAVING
            Both filter, but at different moments. The server's order of work is:

            1. **FROM** picks the table.
            2. **WHERE** keeps or drops individual rows.
            3. **GROUP BY** builds the piles.
            4. Aggregates are computed per pile.
            5. **HAVING** keeps or drops whole piles, using the aggregate results.
            6. **SELECT** shapes the output, then **ORDER BY** sorts it.

            So `WHERE` can say "only orders from this year" (it removes rows before grouping), while `HAVING` can say "only customers whose total is at least 20" (it needs the total, which exists only after grouping). You cannot put an aggregate in WHERE, because the totals are not computed yet. A common mistake is putting a row-level test in HAVING; it works but wastes effort, because it forces the server to build piles it will then throw away.

            ```sql
            SELECT CustomerId, COUNT(*) AS OrderCount, SUM(Total) AS TotalSpent
            FROM Orders
            WHERE OrderDate >= '2026-01-01'          -- rows first
            GROUP BY CustomerId
            HAVING SUM(Total) >= 20                  -- then piles
            ORDER BY TotalSpent DESC;                -- ORDER BY may use the alias
            ```

            Notice HAVING repeats `SUM(Total)` rather than using the alias `TotalSpent`: HAVING runs before SELECT, so the alias does not exist there yet, while ORDER BY runs after SELECT and may use it.

            ## COUNT(DISTINCT ...)
            `COUNT(DISTINCT CustomerId)` counts how many *different* customers appear, ignoring repeats. Compare: ten orders from three customers gives `COUNT(*)` of 10 and `COUNT(DISTINCT CustomerId)` of 3.

            ## A worked example
            Orders: customer 1 has totals 10.50 and 20.00; customer 2 has 5.25 and 7.00; customer 3 has 50.00. Goal: customers who spent 20 or more, biggest spender first.
            1. Group by customer: piles {1: 10.50, 20.00}, {2: 5.25, 7.00}, {3: 50.00}.
            2. Sum each pile: 30.50, 12.25, 50.00. Count rows: 2, 2, 1.
            3. HAVING keeps piles with sum at least 20: customers 1 and 3.
            4. ORDER BY the total descending: customer 3 (50.00), then customer 1 (30.50).

            ## What the words mean
            - **Aggregate function:** collapses many rows into one value (COUNT, SUM, AVG, MIN, MAX).
            - **Group:** the set of rows sharing the same value in the GROUP BY columns.
            - **GROUP BY:** splits rows into groups before aggregating.
            - **HAVING:** filters groups after aggregating (WHERE filters rows before).
            - **COUNT(\*) / COUNT(col):** rows in total / rows where col is not NULL.
            - **DISTINCT inside COUNT:** counts different values only.

            ## Explain it to your team
            "Aggregates turn many rows into one number. GROUP BY makes one summary row per group, and every selected column must be grouped or aggregated. WHERE filters rows before grouping; HAVING filters groups after, because only then do totals exist. Watch NULLs, which aggregates skip, and integer AVG, which drops the fraction."
            """,
            """
            SELECT CustomerId, COUNT(*) AS OrderCount, SUM(Total) AS TotalSpent
            FROM Orders
            GROUP BY CustomerId
            HAVING SUM(Total) >= 20
            ORDER BY TotalSpent DESC;
            """,
            SqlCode(
                "Big spenders",
                "The table is `Orders(OrderId, CustomerId, Total)`. Return one row per customer with three columns: `CustomerId`; `OrderCount`, the number of orders that customer made; and `TotalSpent`, the sum of the customer's order totals. Include only customers whose `TotalSpent` is **20 or more** (exactly 20 counts). Put the biggest spender first.",
                [
                    "Return exactly the columns CustomerId, OrderCount and TotalSpent",
                    "One row per customer, using GROUP BY",
                    "Keep only customers whose total is at least 20, using HAVING",
                    "Sort so the biggest TotalSpent comes first",
                ],
                "-- One row per customer who spent 20 or more, biggest spender first.\nSELECT CustomerId\nFROM Orders\n",
                "Use `COUNT(*)` and `SUM(Total)`, then `GROUP BY CustomerId`, `HAVING SUM(Total) >= 20`, and `ORDER BY TotalSpent DESC`.",
                ["returns CustomerId, OrderCount and TotalSpent", "groups, filters with HAVING and sorts", "still right when the data changes"])
        ),
    ];
}
