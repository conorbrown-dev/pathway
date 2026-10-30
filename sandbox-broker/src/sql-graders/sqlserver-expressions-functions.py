# tests: 3
# Exercise: Name, a Label like 'SENCHA - Tea' (or 'SENCHA - Unsorted' when Category is NULL) and a
# PriceBand (Budget under 5, Standard under 20, Premium otherwise).

SETUP = """
CREATE TABLE Products (ProductId INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL, Category VARCHAR(20) NULL, Price DECIMAL(8,2) NOT NULL);
"""
ROWS_A = "INSERT INTO Products VALUES (1,'Sencha','Tea',12.50),(2,'Mug',NULL,4.99),(3,'Kettle','Gear',45.00);"
ROWS_B = "INSERT INTO Products VALUES (1,'cup','Gear',5.00),(2,'Pot',NULL,19.99),(3,'Urn','Gear',20.00),(4,'Tin','Tea',0.50);"


def attempt(rows):
    session = new_db(SETUP + rows)
    return last_result(run_learner(session), "the products")


def main():
    result = attempt(ROWS_A)
    expect_columns(result.columns, ["Name", "Label", "PriceBand"], "Column names")
    test("returns Name, Label and PriceBand")
    expect_rows(result, [
        ["Sencha", "SENCHA - Tea", "Standard"],
        ["Mug", "MUG - Unsorted", "Budget"],
        ["Kettle", "KETTLE - Gear", "Premium"],
    ], "Rows", ordered=False)
    test("builds the label (upper case, NULL category replaced) and the band")
    expect_rows(attempt(ROWS_B), [
        ["cup", "CUP - Gear", "Standard"],
        ["Pot", "POT - Unsorted", "Standard"],
        ["Urn", "URN - Gear", "Premium"],
        ["Tin", "TIN - Tea", "Budget"],
    ], "With different data (5.00 is Standard, 20.00 is Premium)", ordered=False)
    test("band edges are right")


run(main)
