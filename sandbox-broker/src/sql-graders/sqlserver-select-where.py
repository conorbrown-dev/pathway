# tests: 3
# Exercise: names and prices of Tea products that cost at least 10.

SETUP = """
CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Category VARCHAR(20) NOT NULL,
    Price DECIMAL(8,2) NOT NULL
);
"""
ROWS_A = "INSERT INTO Products (Name, Category, Price) VALUES ('Sencha','Tea',12.50),('Earl Grey','Tea',9.99),('Espresso','Coffee',14.00),('Matcha','Tea',25.00),('Mug','Gear',10.00);"
ROWS_B = "INSERT INTO Products (Name, Category, Price) VALUES ('Oolong','Tea',10.00),('Chai','tea',11.00),('Mate','Tea',9.00),('Pot','Gear',40.00);"


def attempt(rows):
    session = new_db(SETUP + rows)
    return last_result(run_learner(session), "the products")


def main():
    result = attempt(ROWS_A)
    expect_columns(result.columns, ["Name", "Price"], "Column names")
    test("returns Name and Price")
    expect_rows(result, [["Sencha", 12.5], ["Matcha", 25.0]], "Tea products costing 10 or more", ordered=False)
    test("keeps only Tea at 10 or more")
    # Same query, different data: catches answers that hard-code the first data set.
    expect_rows(attempt(ROWS_B), [["Oolong", 10.0], ["Chai", 11.0]], "With different data (10.00 exactly counts, and text matching ignores case)", ordered=False)
    test("still right when the data changes")


run(main)
