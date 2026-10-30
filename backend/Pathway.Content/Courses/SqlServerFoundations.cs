namespace Pathway.Content;

static partial class Curriculum
{
    // Module 1 of the SQL Server course: what a relational database is, how a table stores facts and
    // keeps them trustworthy, and getting a real SQL Server running for the labs.
    private static SqlLessonSpec[] SqlServerFoundationsSpecs() =>
    [
        new(
            "sqlserver-relational-model",
            "SQL Server foundations",
            "What a relational database is, and what SQL Server adds",
            "Why programs keep data in tables instead of loose files, and what a database server does for you.",
            "A relational database stores facts as rows in tables and relates tables to each other through shared values. SQL Server is a database server: a program that owns the data files, answers questions written in the SQL language, and protects the data when many people use it at once or the power fails.",
            [],
            """
            ## The problem a database solves
            Imagine you run a small tea shop and keep your records in a spreadsheet. At first it works. Then it grows: two staff edit it at the same time and one overwrites the other. Someone types "Londn" instead of "London" and now a report misses half your London customers. A customer changes their email and you update it in one sheet but not the other. The file gets too big to open. The laptop dies.

            A **database** is software built to make exactly these problems go away. It keeps your data in a structured form, lets many people read and change it at the same time without stepping on each other, refuses data that breaks your rules, and survives crashes without losing completed work.

            ## Tables, rows and columns
            In a **relational database** all data lives in **tables**. A table is a grid about one kind of thing: a `Customers` table, a `Products` table, an `Orders` table.

            - Each **column** is one fact you record about every item. `Customers` might have the columns `CustomerId`, `Name` and `City`. Every column has a fixed **data type**, such as whole number, text or date, so a date column can never hold the word "banana".
            - Each **row** is one item: one customer, one product, one order.

            ```
            Customers
            CustomerId | Name  | City
            -----------+-------+---------
                     1 | Ada   | London
                     2 | Grace | New York
                     3 | Linus | (empty)
            ```

            The word **relational** comes from the mathematical word "relation", which is simply a table of facts. It does not mean "relationships between tables", though relationships turn out to be the most useful part.

            ## Relating tables to each other
            Do not store the customer's name inside every order. If Ada changes her name you would have to find and fix every order. Instead, give each customer a unique number and store only that number in the order:

            ```
            Orders
            OrderId | CustomerId | Total
            --------+------------+------
                 10 |          1 | 10.50     (this is Ada's order)
                 11 |          1 | 20.00     (also Ada's)
                 12 |          2 |  5.25     (Grace's)
            ```

            The `CustomerId` in `Orders` points at a row in `Customers`. A fact is now written in exactly one place, and any question that needs both tables (for example "show each order with the customer's name") is answered by **joining** them: matching rows up by that shared number. Joins are the heart of Module 3.

            ## What SQL is
            **SQL** (Structured Query Language, said "sequel" or "S-Q-L") is the language you use to talk to a relational database. It is **declarative**: you describe *what* result you want, not *how* to compute it. You write "give me the names of customers in London"; you do not write the loop that scans the table. The database picks the method. That choice is made by a part of the server called the query optimizer, which later lessons open up.

            SQL is a standard shared by many products (SQL Server, PostgreSQL, MySQL, SQLite, Oracle), but each product adds its own extensions and quirks. **T-SQL** (Transact-SQL) is Microsoft's dialect used by SQL Server. Everything you learn about tables, rows and joins carries to other databases; the exact spelling of some functions does not.

            ## What SQL Server adds
            People say "SQL Server" for the whole product. Strictly it is a **database server**: a long-running program that:

            - **Owns the data files.** Programs never open the files. They connect to the server over a network connection and send it SQL text.
            - **Answers requests.** It reads your SQL, decides the cheapest way to answer, and sends back rows.
            - **Protects the data.** It enforces rules you declare, such as "this column is never empty", and it groups changes into **transactions** so a half-finished change can be undone.
            - **Handles many users at once.** It locks and versions data so two people editing at the same moment do not corrupt each other's work.
            - **Recovers from failure.** Every change is first written to a **transaction log**, so after a crash the server can replay finished work and discard unfinished work.
            - **Is administered.** Backups, security and scheduled jobs are part of the product.

            Tools such as **SQL Server Management Studio (SSMS)** are just programs that connect to a server and send SQL, like any application of yours would.

            ## Terms you will keep seeing
            - **Instance:** one running copy of the SQL Server program. A machine can run several.
            - **Database:** a named container inside an instance, holding tables and other objects. One instance holds many databases.
            - **Schema:** a named folder inside a database that groups objects. The default one is `dbo` ("database owner"), so `dbo.Customers` and `Customers` usually mean the same table.
            - **Query:** a SQL statement that reads data. **Statement:** any single SQL command.
            - **Batch:** a group of statements sent to the server together. In tools, the word `GO` on its own line ends a batch. `GO` is a tool command, not T-SQL.

            ## What the words mean
            - **Table:** a grid of data about one kind of thing.
            - **Row:** one item in a table. **Column:** one fact recorded about every item.
            - **Data type:** the kind of value a column may hold (whole number, text, date).
            - **Relational:** organized as tables of facts that can be matched up by shared values.
            - **Join:** matching rows from two tables by a shared value.
            - **SQL / T-SQL:** the standard language for relational databases / Microsoft's dialect of it.
            - **Declarative:** you say what result you want and the database decides how to get it.
            - **Transaction:** a group of changes that either all happen or none of them do.
            - **Transaction log:** the server's record of every change, used to recover after a crash.

            ## Honest limits
            Relational databases are not the answer to every problem. Very large streams of events, free-form documents, graphs and caches each have tools built for them, and SQL Server can store JSON and graphs but is not always the best fit. A good engineer picks the tool by the shape of the data and the questions asked, which is something you will be able to defend by the end of this course.

            ## Explain it to your team
            "A relational database keeps data in tables of rows and columns, and links tables through shared ids so each fact is stored once. SQL is the language where you say what you want and the database works out how. SQL Server is the server program that owns the files, enforces our rules, lets many people work at once and recovers from crashes. T-SQL is its dialect."
            """,
            """
            Customers                         Orders
            CustomerId | Name  | City         OrderId | CustomerId | Total
                     1 | Ada   | London            10 |          1 | 10.50
                     2 | Grace | New York          11 |          1 | 20.00
                                                   12 |          2 |  5.25

            Orders.CustomerId points at Customers.CustomerId.
            """,
            SqlChoice(
                "Where does a fact live?",
                "A shop's Orders table repeats each customer's full name and email on every order. Ada changes her email address. What is the main problem with this design, and the usual fix?",
                "split",
                [
                    new("split", "The email must be changed on every one of her orders. Store it once in a Customers table and have each order hold only her CustomerId"),
                    new("faster", "Nothing is wrong; repeating data makes reports faster, so keep it"),
                    new("delete", "Delete her old orders so only the newest copy of her email remains"),
                ],
                "Ask how many places a single fact is written, and how many you would have to fix when it changes.",
                "Explains why related tables beat repeated data")
        ),
        new(
            "sqlserver-tables-keys",
            "SQL Server foundations",
            "Tables, data types, keys and constraints",
            "Write CREATE TABLE, choose data types, and use primary keys, UNIQUE and NOT NULL so the database refuses bad data.",
            "CREATE TABLE names each column with a data type. A PRIMARY KEY gives every row a unique, never-empty identity; UNIQUE forbids repeated values in a column; NOT NULL forbids a missing value. These constraints let the database itself reject data that breaks your rules, so no program can sneak bad rows in.",
            [("sqlserver-relational-model", "you know what a table, a row, a column and a data type are, and why rows are linked by ids.")],
            """
            ## Describing a table
            Before any data goes in you tell SQL Server what the table looks like. The statement is `CREATE TABLE`. Read it as a recipe: a table name, then a bracketed list of columns, each with a name, a data type and optional rules.

            ```sql
            CREATE TABLE Customers (
                CustomerId INT           NOT NULL PRIMARY KEY,
                Email      VARCHAR(100)  NOT NULL UNIQUE,
                Name       NVARCHAR(50)  NOT NULL,
                City       VARCHAR(30)   NULL
            );
            ```

            Everything after the column's type is a rule. Nothing about it says how to *store* the rows; you only say what a valid row looks like, and the server enforces it every time a row is added or changed.

            ## Data types, in words
            A data type decides what values a column can hold and how much room each takes. The ones you need first:

            - **INT:** a whole number from about −2.1 billion to +2.1 billion. Use `BIGINT` when you could pass that, `SMALLINT` or `TINYINT` when values are small.
            - **DECIMAL(p, s):** an exact number with fractions, written for example `DECIMAL(10,2)`. `p` is the **precision**, the total number of digits kept; `s` is the **scale**, how many of those sit after the decimal point. `DECIMAL(10,2)` holds up to 99,999,999.99. Use it for money. Do not use `FLOAT` for money: `FLOAT` stores a very close approximation, and approximations add up to wrong cents.
            - **VARCHAR(n) and NVARCHAR(n):** text of up to `n` characters. The `N` means Unicode, so it can hold any language, emoji included; plain `VARCHAR` is limited by the database's code page. If people from anywhere may type into it, use `NVARCHAR`. Write text values with single quotes: `'London'`. A Unicode text value is written `N'Zoë'`.
            - **DATE, TIME, DATETIME2:** a calendar date; a time of day; a date and time together with fractional seconds. Prefer `DATETIME2` over the older `DATETIME`.
            - **BIT:** 0 or 1, used for yes/no flags. There is no true boolean type in T-SQL.

            ## NULL: "no value"
            `NULL` is not zero and not an empty text. It means **the value is unknown or missing**. A column marked `NULL` may be left without a value; marked `NOT NULL`, the server refuses a row that omits it. Make a column `NOT NULL` whenever a row without it would make no sense. NULL is trickier than it looks, so the whole of Lesson 5 is about it.

            ## Primary keys: a row's identity
            A **primary key** is the column (or columns) that identifies one row for all time. The server enforces two rules at once: values must be **unique**, and they can never be NULL. Without a key you can have two rows that look identical and no way to say which one you mean when you update or delete.

            Choose keys that never change and carry no meaning. A customer's email is a bad key because people change email; a made-up number (`CustomerId`) is a good one. That is called a **surrogate key**. SQL Server can generate these for you with `IDENTITY(1,1)`, meaning "start at 1 and go up by 1", so you do not have to invent the numbers yourself.

            ## UNIQUE, and why it differs from a key
            `UNIQUE` also forbids repeated values, but the column may be empty (once, in SQL Server). Use it for things that must not repeat yet are not the row's identity: an email address, a book's ISBN, a username. A table has one primary key and may have many UNIQUE rules.

            ## Constraints do the policing
            Primary keys, `UNIQUE` and `NOT NULL` are **constraints**: rules the database checks on every change. This is the most important design idea in this lesson. Do not rely on your application code to keep data clean. Applications get rewritten, scripts run directly against the table, and bugs happen. A constraint cannot be bypassed by any of them.

            Here is what it looks like to break the rules:

            ```sql
            INSERT INTO Customers (CustomerId, Email, Name) VALUES (1, 'ada@example.test', N'Ada');   -- works
            INSERT INTO Customers (CustomerId, Email, Name) VALUES (1, 'x@example.test', N'Bob');     -- fails: CustomerId 1 already exists
            INSERT INTO Customers (CustomerId, Email, Name) VALUES (2, 'ada@example.test', N'Eve');   -- fails: Email must be unique
            INSERT INTO Customers (CustomerId, Email, Name) VALUES (3, 'z@example.test', NULL);       -- fails: Name is NOT NULL
            ```

            Each failing statement stops with an error and changes nothing. (`INSERT` adds a row; it is explained properly in Module 5. Here you only need to see that the table refuses bad rows.)

            ## Naming
            Pick one style and stay with it. This course uses singular or plural table names in PascalCase (`Customers`, `OrderLines`), and column names in PascalCase (`CustomerId`). SQL Server is not case-sensitive for names by default, so `customers` and `Customers` are the same table. If a name must contain spaces or be a reserved word, wrap it in square brackets: `[Order]`.

            ## What the words mean
            - **CREATE TABLE:** the statement that defines a new table.
            - **Constraint:** a rule the database enforces on every change to the table.
            - **PRIMARY KEY:** the unique, never-empty identity of each row.
            - **UNIQUE:** no two rows may share this value.
            - **NOT NULL:** the value may not be missing.
            - **NULL:** unknown or missing value (not zero, not empty text).
            - **IDENTITY:** a column whose values the server generates, counting up.
            - **Surrogate key:** a made-up id with no business meaning, used as the primary key.
            - **Precision / scale:** total digits kept / digits after the decimal point.

            ## Explain it to your team
            "A table is defined once with CREATE TABLE, naming each column and its data type. The primary key identifies each row; UNIQUE and NOT NULL stop duplicates and missing values. Because the database enforces these constraints itself, no application bug or ad hoc script can put bad data in. We use DECIMAL for money, NVARCHAR for text that people type, and generated surrogate keys."
            """,
            """
            CREATE TABLE Customers (
                CustomerId INT          NOT NULL PRIMARY KEY,
                Email      VARCHAR(100) NOT NULL UNIQUE,
                Name       NVARCHAR(50) NOT NULL,
                City       VARCHAR(30)  NULL
            );
            """,
            SqlCode(
                "Design a Books table",
                "Create a table named `Books` for a library. It needs four columns, in this order: `BookId` (a whole number that identifies the book), `Isbn` (text up to 20 characters, never missing, never repeated), `Title` (text up to 200 characters, never missing) and `Pages` (a whole number that may be missing). Your script is run against an empty database and then the grader tries to insert good and bad rows.",
                [
                    "Create a table named `Books` with columns BookId, Isbn, Title and Pages, in that order",
                    "BookId is the PRIMARY KEY",
                    "Isbn is NOT NULL and UNIQUE",
                    "Title is NOT NULL, and Pages allows NULL",
                ],
                "-- Write your CREATE TABLE statement here.\n",
                "Each column is `Name TYPE rules`. For example `Isbn VARCHAR(20) NOT NULL UNIQUE`. Pages simply uses `INT NULL`.",
                ["Books has the four columns in order", "accepts a normal row", "rejects a repeated key and a repeated ISBN", "requires Title but lets Pages stay empty"])
        ),
        new(
            "sqlserver-install-lab",
            "SQL Server foundations",
            "Lab: run SQL Server on your own machine",
            "Install SQL Server Developer in Docker and connect with SSMS or Azure Data Studio, so the later labs have a real server to use.",
            "The sandbox in this course can check the T-SQL you write, but it is not SQL Server. For the topics that depend on the real engine (execution plans, locking, backups, security) you run a free SQL Server Developer instance yourself, in a container, and connect to it with a tool that sends T-SQL. This lesson gets that working and proves it.",
            [
                ("sqlserver-relational-model", "you know a server owns the data and tools such as SSMS are just clients that send it SQL."),
                ("sqlserver-tables-keys", "you can read a CREATE TABLE statement, which you will run against your own server."),
            ],
            """
            ## Why a lab at all
            The editor in this course reads your T-SQL and runs it against a stand-in database so it can check your answers instantly. That is perfect for learning how queries behave. It cannot show you how the real engine chooses a plan, takes locks, writes a backup or enforces permissions, because those are features of the real program. For those lessons you run your own server. SQL Server **Developer edition** is free and has all the features of the paid Enterprise edition, licensed for development and testing, never for production.

            ## What you will set up
            Three pieces, each doing one job:
            1. **A SQL Server instance**, the actual server. The easiest way on any laptop is a **container**: a packaged, isolated copy of the program that you start with one command and throw away when done. You need Docker Desktop (Windows, macOS, Linux) or Podman.
            2. **A client tool** to send queries and see results. SQL Server Management Studio (**SSMS**) is the full-featured tool and runs on Windows only. **Azure Data Studio** was a lighter cross-platform alternative, but check Microsoft's current guidance because tooling here has been changing; the **VS Code** SQL extension and the command-line `sqlcmd` also work on every system.
            3. **A sample database** to practice on. You will create your own in the exercise, so nothing needs downloading.

            ## Steps
            1. Install Docker Desktop (or Podman) and confirm `docker --version` prints a version.
            2. Start the server. The official image is published by Microsoft. A typical command looks like this (check Microsoft's documentation for the current image tag and any changes to the options):
            ```bash
            docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<choose-a-strong-password>" \
              -p 1433:1433 --name sql-lab -d mcr.microsoft.com/mssql/server:2025-latest
            ```
               - `ACCEPT_EULA=Y` accepts the license. `MSSQL_SA_PASSWORD` sets the password of the built-in administrator account `sa`; it must be long and mixed-case with digits and symbols or the server refuses to start.
               - `-p 1433:1433` forwards port 1433, SQL Server's usual port, from the container to your machine.
               - On Apple Silicon Macs the image runs under emulation; allow extra memory in Docker settings (at least 2 GB).
            3. Check it started: `docker logs sql-lab` should end with a line saying SQL Server is ready for client connections.
            4. Connect with your client to `localhost,1433` (a comma, not a colon, separates host and port in SQL Server tools), login `sa`, and the password you chose. For a local lab, tick "Trust server certificate".
            5. Run this and read the answer:
            ```sql
            SELECT @@VERSION;
            ```
               `SELECT` asks the server for a value, and `@@VERSION` is a built-in value holding its version text. The answer should mention SQL Server and its release.
            6. Create a practice database and a first table (you wrote this kind of statement in Lesson 2):
            ```sql
            CREATE DATABASE Pathway;
            GO
            USE Pathway;
            GO
            CREATE TABLE Books (BookId INT NOT NULL PRIMARY KEY, Title NVARCHAR(200) NOT NULL);
            ```
            `GO` ends a batch in your client tool. `USE Pathway` makes the new database the current one.

            ## Safety notes
            - Keep this server for learning. Never put real customer data in it, and never expose port 1433 to the internet.
            - The `sa` password in a command line is visible in your shell history. That is fine for a throwaway lab and wrong for anything real; real systems use secrets managers and named logins.
            - When you are done, `docker stop sql-lab` pauses it and `docker rm sql-lab` deletes it and its data.

            ## What the words mean
            - **Instance:** one running copy of the server program.
            - **Container:** an isolated, packaged copy of a program you can start and delete cleanly.
            - **Image:** the template a container is started from.
            - **Port:** a numbered door on a machine that a program listens at; SQL Server's is 1433.
            - **sa:** the built-in administrator login ("system administrator").
            - **Client tool:** a program such as SSMS that sends T-SQL to a server and shows the results.
            - **Developer edition:** the free, full-featured edition licensed for non-production use.
            - **Batch / GO:** a group of statements sent together / the client-tool word that ends one.

            ## Explain it to your team
            "The in-course editor checks T-SQL logic, but the engine-specific labs need a real server. We run SQL Server Developer in a container, connect with SSMS or another client over port 1433, and prove it works with SELECT @@VERSION and a first table. It is for learning only: free for non-production use, with throwaway credentials."
            """,
            """
            docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<strong-password>" -p 1433:1433 --name sql-lab -d mcr.microsoft.com/mssql/server:2025-latest

            SELECT @@VERSION;
            """,
            SqlLab(
                "Present your working lab",
                "Get SQL Server Developer running, connect to it, and explain to a teammate what each piece does. Keep the evidence for your portfolio.",
                [
                    "A screenshot or pasted output of `SELECT @@VERSION;` from your own server",
                    "The exact command or settings you used to start the instance, with the password removed",
                    "Which client tool you connected with and how you entered the server address",
                    "A short note on one thing that failed (for example a password rule or port conflict) and how you fixed it",
                    "Two sentences you could say out loud: the difference between the instance, the database and the client tool",
                ],
                "If it will not start, run `docker logs sql-lab`. The most common cause is a password that does not meet the complexity rules.")
        ),
    ];
}
