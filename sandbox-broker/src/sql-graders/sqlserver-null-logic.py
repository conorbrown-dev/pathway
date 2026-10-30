# tests: 3
# Exercise: return the name and city of every active customer who is not in London, newest id
# first. Teaches NULL-aware comparison (a NULL city is not "not London" to SQL) and IS NULL.
SETUP = """
CREATE TABLE Customers (
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    City VARCHAR(30) NULL,
    Active BIT NOT NULL DEFAULT 1
);
INSERT INTO Customers (Name, City, Active) VALUES
  ('Ada', 'London', 1), ('Grace', 'New York', 1), ('Linus', NULL, 1),
  ('Edsger', 'Austin', 0), ('Barbara', 'Boston', 1);
"""


def main():
    session = new_db(SETUP)
    result = last_result(run_learner(session), "the customers")
    expect_columns(result.columns, ["Name", "City"], "Column names")
    test("returns Name and City")
    got = [list(r) for r in result]
    check(all(r[0] != "Ada" for r in got), "Ada lives in London, so she should be left out.")
    check(all(r[0] != "Edsger" for r in got), "Edsger is not active, so he should be left out.")
    test("filters on Active and City")
    expect_rows(got, [["Barbara", "Boston"], ["Linus", None], ["Grace", "New York"]], "Rows")
    test("keeps the customer whose City is NULL, newest first")


run(main)
