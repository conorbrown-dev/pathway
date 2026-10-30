# tests: 3
# Exercise: every customer with each of their orders (Name, OrderId, Total); customers with no
# orders still appear once, with NULLs.

SETUP = """
CREATE TABLE Customers (CustomerId INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL);
CREATE TABLE Orders (OrderId INT PRIMARY KEY, CustomerId INT NOT NULL REFERENCES Customers(CustomerId), Total DECIMAL(10,2) NOT NULL);
"""
ROWS_A = "INSERT INTO Customers VALUES (1,'Ada'),(2,'Grace'),(3,'Linus'); INSERT INTO Orders VALUES (10,1,10.5),(11,1,20),(12,2,5.25);"
ROWS_B = "INSERT INTO Customers VALUES (1,'Zoe'),(2,'Yan'); INSERT INTO Orders VALUES (5,2,9),(6,2,8);"


def attempt(rows):
    session = new_db(SETUP + rows)
    return last_result(run_learner(session), "customers with their orders")


def main():
    result = attempt(ROWS_A)
    expect_columns(result.columns, ["Name", "OrderId", "Total"], "Column names")
    test("returns Name, OrderId and Total")
    expect_rows(result, [["Ada", 10, 10.5], ["Ada", 11, 20.0], ["Grace", 12, 5.25], ["Linus", None, None]], "Every customer, with orders where they exist", ordered=False)
    test("keeps the customer who has no orders")
    expect_rows(attempt(ROWS_B), [["Zoe", None, None], ["Yan", 5, 9.0], ["Yan", 6, 8.0]], "With different data", ordered=False)
    test("still right when the data changes")


run(main)
