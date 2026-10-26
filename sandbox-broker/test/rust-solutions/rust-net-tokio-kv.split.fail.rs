use std::collections::HashMap;
use std::sync::{Arc, Mutex};
use tokio::io::{AsyncBufReadExt, AsyncWriteExt, BufReader};
use tokio::net::{TcpListener, TcpStream};

type Store = Arc<Mutex<HashMap<String, String>>>;

// Splits on every space, so a value containing spaces is rejected.
fn apply(command: &str, store: &Store) -> String {
    let parts: Vec<&str> = command.split_whitespace().collect();
    let mut map = store.lock().unwrap();
    match parts.as_slice() {
        ["SET", key, value] => {
            map.insert(key.to_string(), value.to_string());
            String::from("OK")
        }
        ["GET", key] => map.get(*key).map(|v| format!("VALUE {v}")).unwrap_or_else(|| String::from("NOT_FOUND")),
        ["DEL", key] => map.remove(*key).map(|_| String::from("DELETED")).unwrap_or_else(|| String::from("NOT_FOUND")),
        ["COUNT"] => format!("COUNT {}", map.len()),
        _ => String::from("ERR unknown command"),
    }
}

async fn handle(stream: TcpStream, store: Store) -> std::io::Result<()> {
    let (reader, mut writer) = stream.into_split();
    let mut lines = BufReader::new(reader).lines();
    while let Some(line) = lines.next_line().await? {
        writer.write_all(format!("{}\n", apply(&line, &store)).as_bytes()).await?;
    }
    Ok(())
}

async fn serve(listener: TcpListener, store: Store) {
    loop {
        if let Ok((stream, _)) = listener.accept().await {
            let store = store.clone();
            tokio::spawn(async move { handle(stream, store).await });
        }
    }
}
