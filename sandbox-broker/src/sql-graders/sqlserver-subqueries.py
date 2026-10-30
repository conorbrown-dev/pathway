# tests: 3
# Exercise: Name, Category, Price of products that cost more than the average price of their own category.

SETUP = """
CREATE TABLE Products (ProductId INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL, Category VARCHAR(20) NOT NULL, Price DECIMAL(8,2) NOT NULL);
"""
ROWS_A = "INSERT INTO Products VALUES (1,'Sencha','Tea',12),(2,'Earl Grey','Tea',8),(3,'Matcha','Tea',25),(4,'Mug','Gear',10),(5,'Kettle','Gear',40);"
ROWS_B = "INSERT INTO Products VALUES (1,'a','X',10),(2,'b','X',10),(3,'c','Y',1),(4,'d','Y',3),(5,'e','Z',7);"


def attempt(rows):
    session = new_db(SETUP + rows)
    return last_result(run_learner(session), "the products")


def main():
    result = attempt(ROWS_A)
    expect_columns(result.columns, ["Name", "Category", "Price"], "Column names")
    test("returns Name, Category and Price")
    expect_rows(result, [["Matcha", "Tea", 25.0], ["Kettle", "Gear", 40.0]], "Products above their own category's average", ordered=False)
    test("compares each product with its own category's average")
    expect_rows(attempt(ROWS_B), [["d", "Y", 3.0]], "With different data (equal to the average is not above it)", ordered=False)
    test("still right when the data changes")


run(main)
