# tests: 3
# Exercise: page 2 (3 rows per page) of products, most expensive first, ties broken by Name.

SETUP = """
CREATE TABLE Products (ProductId INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL, Price DECIMAL(8,2) NOT NULL);
"""
ROWS_A = "INSERT INTO Products VALUES (1,'A',5),(2,'B',9),(3,'C',7),(4,'D',3),(5,'E',8),(6,'F',6),(7,'G',4),(8,'H',2);"
ROWS_B = "INSERT INTO Products VALUES (1,'Zed',5),(2,'Amy',5),(3,'Bob',5),(4,'Cat',5),(5,'Dan',9),(6,'Eve',5),(7,'Fay',1);"


def attempt(rows):
    session = new_db(SETUP + rows)
    return last_result(run_learner(session), "a page of products")


def main():
    result = attempt(ROWS_A)
    expect_columns(result.columns, ["Name", "Price"], "Column names")
    test("returns Name and Price")
    expect_rows(result, [["F", 6], ["A", 5], ["G", 4]], "Page 2 (rows 4 to 6 of the whole sorted list)")
    test("skips the first page and returns three rows in price order")
    expect_rows(attempt(ROWS_B), [["Cat", 5], ["Eve", 5], ["Zed", 5]], "With many equal prices, ties must be ordered by Name")
    test("breaks ties by Name")


run(main)
