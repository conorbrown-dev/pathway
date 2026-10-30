# tests: 3
# Exercise: per customer: CustomerId, OrderCount, TotalSpent, only customers who spent 20 or more,
# biggest spender first.

SETUP = """
CREATE TABLE Orders (OrderId INT PRIMARY KEY, CustomerId INT NOT NULL, Total DECIMAL(10,2) NOT NULL);
"""
ROWS_A = "INSERT INTO Orders VALUES (1,1,10.50),(2,1,20.00),(3,2,5.25),(4,2,7.00),(5,3,50.00);"
ROWS_B = "INSERT INTO Orders VALUES (1,7,10),(2,7,10),(3,8,19.99),(4,9,100),(5,9,0.5),(6,9,1);"


def attempt(rows):
    session = new_db(SETUP + rows)
    return last_result(run_learner(session), "a row per customer")


def main():
    result = attempt(ROWS_A)
    expect_columns(result.columns, ["CustomerId", "OrderCount", "TotalSpent"], "Column names")
    test("returns CustomerId, OrderCount and TotalSpent")
    expect_rows(result, [[3, 1, 50.0], [1, 2, 30.5]], "Customers who spent 20 or more, biggest spender first")
    test("groups, filters with HAVING and sorts")
    expect_rows(attempt(ROWS_B), [[9, 3, 101.5], [7, 2, 20.0]], "With different data (exactly 20 counts, 19.99 does not)")
    test("still right when the data changes")


run(main)
