use rusqlite::{Connection, OptionalExtension, params};

#[derive(Debug, PartialEq)]
enum TransferError {
    InvalidAmount,
    NoSuchAccount,
    InsufficientFunds,
    Database(String),
}

impl From<rusqlite::Error> for TransferError {
    fn from(error: rusqlite::Error) -> Self {
        TransferError::Database(error.to_string())
    }
}

fn migrate(conn: &Connection) -> rusqlite::Result<()> {
    conn.execute_batch(
        "CREATE TABLE IF NOT EXISTS accounts (id INTEGER PRIMARY KEY, owner TEXT NOT NULL UNIQUE, balance INTEGER NOT NULL CHECK (balance >= 0));
         CREATE TABLE IF NOT EXISTS ledger (id INTEGER PRIMARY KEY, from_id INTEGER NOT NULL, to_id INTEGER NOT NULL, amount INTEGER NOT NULL);
         PRAGMA user_version = 2;",
    )
}

fn open_account(conn: &Connection, owner: &str, balance: i64) -> rusqlite::Result<i64> {
    conn.execute("INSERT INTO accounts (owner, balance) VALUES (?1, ?2)", params![owner, balance])?;
    Ok(conn.last_insert_rowid())
}

fn balance(conn: &Connection, owner: &str) -> rusqlite::Result<Option<i64>> {
    conn.query_row("SELECT balance FROM accounts WHERE owner = ?1", params![owner], |row| row.get(0)).optional()
}

// No transaction: the debit happens even when the credit finds no account.
fn transfer(conn: &mut Connection, from: &str, to: &str, amount: i64) -> Result<(), TransferError> {
    if amount <= 0 {
        return Err(TransferError::InvalidAmount);
    }
    let debited = conn.execute("UPDATE accounts SET balance = balance - ?1 WHERE owner = ?2", params![amount, from]).map_err(|_| TransferError::InsufficientFunds)?;
    if debited == 0 {
        return Err(TransferError::NoSuchAccount);
    }
    let credited = conn.execute("UPDATE accounts SET balance = balance + ?1 WHERE owner = ?2", params![amount, to])?;
    if credited == 0 {
        return Err(TransferError::NoSuchAccount);
    }
    conn.execute("INSERT INTO ledger (from_id, to_id, amount) VALUES (0, 0, ?1)", params![amount])?;
    Ok(())
}
