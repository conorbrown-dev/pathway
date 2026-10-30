# tests: 3
# Exercise: one column, City: cities that have employees but no contractors, alphabetical, no repeats.

SETUP = """
CREATE TABLE Employees (EmployeeId INT PRIMARY KEY, City VARCHAR(30) NOT NULL);
CREATE TABLE Contractors (ContractorId INT PRIMARY KEY, City VARCHAR(30) NOT NULL);
"""
ROWS_A = "INSERT INTO Employees VALUES (1,'Tulsa'),(2,'Tulsa'),(3,'Austin'),(4,'Boston'); INSERT INTO Contractors VALUES (1,'Boston'),(2,'Denver');"
ROWS_B = "INSERT INTO Employees VALUES (1,'Oslo'),(2,'Oslo'),(3,'Rome'); INSERT INTO Contractors VALUES (1,'Rome'),(2,'Rome'),(3,'Paris');"


def attempt(rows):
    session = new_db(SETUP + rows)
    return last_result(run_learner(session), "a list of cities")


def main():
    result = attempt(ROWS_A)
    expect_columns(result.columns, ["City"], "Column names")
    test("returns a single City column")
    expect_rows(result, [["Austin"], ["Tulsa"]], "Cities with employees but no contractors, A to Z, each once")
    test("subtracts contractor cities and removes repeats")
    expect_rows(attempt(ROWS_B), [["Oslo"]], "With different data")
    test("still right when the data changes")


run(main)
