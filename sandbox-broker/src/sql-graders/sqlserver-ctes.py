# tests: 3
# Exercise: use a CTE named CustomerTotals (CustomerId, Spent) and return Name, Spent for customers
# whose Spent is above the average Spent across customers who have orders, biggest first.

SETUP = """
CREATE TABLE Customers (CustomerId INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL);
CREATE TABLE Orders (OrderId INT PRIMARY KEY, CustomerId INT NOT NULL, Total DECIMAL(10,2) NOT NULL);
"""
ROWS_A = "INSERT INTO Customers VALUES (1,'Ada'),(2,'Grace'),(3,'Linus'),(4,'Idle'); INSERT INTO Orders VALUES (1,1,10),(2,1,20),(3,2,5),(4,3,60);"
ROWS_B = "INSERT INTO Customers VALUES (1,'P'),(2,'Q'),(3,'R'); INSERT INTO Orders VALUES (1,1,10),(2,2,10),(3,3,40);"


def attempt(rows):
    session = new_db(SETUP + rows)
    return last_result(run_learner(session), "the big spenders")


def main():
    text = load_submission()
    check("customertotals" in text.lower().replace("[", "").replace("]", ""), "Name your CTE CustomerTotals, as the exercise asks.")
    test("uses a CTE called CustomerTotals")
    result = attempt(ROWS_A)
    expect_columns(result.columns, ["Name", "Spent"], "Column names")
    expect_rows(result, [["Linus", 60.0]], "Customers above the average of customers who have orders (the average is (30+5+60)/3, not counting Idle)")
    test("compares against the average of customers with orders")
    expect_rows(attempt(ROWS_B), [["R", 40.0]], "With different data")
    test("still right when the data changes")


run(main)
