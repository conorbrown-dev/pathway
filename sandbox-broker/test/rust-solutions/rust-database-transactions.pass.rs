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
    let version: i64 = conn.query_row("PRAGMA user_version", [], |row| row.get(0))?;
    if version < 1 {
        conn.execute_batch(
            "BEGIN;
             CREATE TABLE accounts (
                 id INTEGER PRIMARY KEY,
                 owner TEXT NOT NULL UNIQUE,
                 balance INTEGER NOT NULL CHECK (balance >= 0)
             );
             PRAGMA user_version = 1;
             COMMIT;",
        )?;
    }
    if version < 2 {
        conn.execute_batch(
            "BEGIN;
             CREATE TABLE ledger (
                 id INTEGER PRIMARY KEY,
                 from_id INTEGER NOT NULL REFERENCES accounts(id),
                 to_id INTEGER NOT NULL REFERENCES accounts(id),
                 amount INTEGER NOT NULL CHECK (amount > 0)
             );
             PRAGMA user_version = 2;
             COMMIT;",
        )?;
    }
    Ok(())
}

fn open_account(conn: &Connection, owner: &str, balance: i64) -> rusqlite::Result<i64> {
    conn.execute("INSERT INTO accounts (owner, balance) VALUES (?1, ?2)", params![owner, balance])?;
    Ok(conn.last_insert_rowid())
}

fn balance(conn: &Connection, owner: &str) -> rusqlite::Result<Option<i64>> {
    conn.query_row("SELECT balance FROM accounts WHERE owner = ?1", params![owner], |row| row.get(0)).optional()
}

fn transfer(conn: &mut Connection, from: &str, to: &str, amount: i64) -> Result<(), TransferError> {
    if amount <= 0 {
        return Err(TransferError::InvalidAmount);
    }
    let tx = conn.transaction()?;
    let source: Option<(i64, i64)> = tx
        .query_row("SELECT id, balance FROM accounts WHERE owner = ?1", params![from], |row| Ok((row.get(0)?, row.get(1)?)))
        .optional()?;
    let target: Option<i64> = tx.query_row("SELECT id FROM accounts WHERE owner = ?1", params![to], |row| row.get(0)).optional()?;
    let (Some((from_id, from_balance)), Some(to_id)) = (source, target) else {
        return Err(TransferError::NoSuchAccount); // dropping `tx` rolls back
    };
    if from_balance < amount {
        return Err(TransferError::InsufficientFunds);
    }
    tx.execute("UPDATE accounts SET balance = balance - ?1 WHERE id = ?2", params![amount, from_id])?;
    tx.execute("UPDATE accounts SET balance = balance + ?1 WHERE id = ?2", params![amount, to_id])?;
    tx.execute("INSERT INTO ledger (from_id, to_id, amount) VALUES (?1, ?2, ?3)", params![from_id, to_id, amount])?;
    tx.commit()?;
    Ok(())
}
