-- expect: Title should be NOT NULL
CREATE TABLE Books (
    BookId INT NOT NULL PRIMARY KEY,
    Isbn   VARCHAR(20) NOT NULL UNIQUE,
    Title  NVARCHAR(200) NULL,
    Pages  INT NULL
);
