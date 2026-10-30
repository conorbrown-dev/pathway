# tests: 4
# Exercise: create a Books table with a primary key, a required title and a unique ISBN.


def main():
    session = new_db("")
    run_learner(session)
    check(session.has_table("Books"), "No table named Books exists. Start with CREATE TABLE Books (...).")
    expect_columns(session.columns("Books"), ["BookId", "Isbn", "Title", "Pages"], "Books columns")
    test("Books has the four columns, in order")

    session.run("INSERT INTO Books (BookId, Isbn, Title, Pages) VALUES (1, '978-0', 'First', 120);")
    check(not session.errors, f"A normal row was refused: {session.errors[:1]}.")
    check(scalar(session, "SELECT COUNT(*) FROM Books;") == 1, "The first valid row should have been stored.")
    test("accepts a normal row")

    session.run("INSERT INTO Books (BookId, Isbn, Title, Pages) VALUES (1, '978-1', 'Same id', 10);")
    check(session.errors, "Two books were allowed to share a BookId. BookId should be the PRIMARY KEY.")
    session.errors.clear()
    session.run("INSERT INTO Books (BookId, Isbn, Title, Pages) VALUES (2, '978-0', 'Same isbn', 10);")
    check(session.errors, "Two books were allowed to share an Isbn. Isbn should be UNIQUE.")
    session.errors.clear()
    test("rejects a repeated key and a repeated ISBN")

    session.run("INSERT INTO Books (BookId, Isbn, Title, Pages) VALUES (3, '978-3', NULL, 10);")
    check(session.errors, "A book with no Title was allowed. Title should be NOT NULL.")
    session.errors.clear()
    session.run("INSERT INTO Books (BookId, Isbn, Title) VALUES (4, '978-4', 'No pages');")
    check(not session.errors, "Pages should be allowed to be missing (NULL), but leaving it out was refused.")
    test("requires Title but lets Pages stay empty")


run(main)
