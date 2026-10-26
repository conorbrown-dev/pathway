use std::collections::HashMap;
use std::sync::{Arc, Mutex};
use tokio::io::{AsyncBufReadExt, AsyncWriteExt, BufReader};
use tokio::net::{TcpListener, TcpStream};

type Store = Arc<Mutex<HashMap<String, String>>>;

fn apply(command: &str, store: &Store) -> String {
    let mut parts = command.trim().splitn(3, ' ');
    let mut map = store.lock().unwrap();
    match (parts.next(), parts.next(), parts.next()) {
        (Some("SET"), Some(key), Some(value)) => {
            map.insert(key.to_string(), value.to_string());
            String::from("OK")
        }
        (Some("GET"), Some(key), None) => match map.get(key) {
            Some(value) => format!("VALUE {value}"),
            None => String::from("NOT_FOUND"),
        },
        (Some("DEL"), Some(key), None) => match map.remove(key) {
            Some(_) => String::from("DELETED"),
            None => String::from("NOT_FOUND"),
        },
        (Some("COUNT"), None, None) => format!("COUNT {}", map.len()),
        _ => String::from("ERR unknown command"),
    }
}

async fn handle(stream: TcpStream, store: Store) -> std::io::Result<()> {
    let (reader, mut writer) = stream.into_split();
    let mut lines = BufReader::new(reader).lines();
    while let Some(line) = lines.next_line().await? {
        let reply = apply(&line, &store);
        writer.write_all(reply.as_bytes()).await?;
        writer.write_all(b"\n").await?;
    }
    Ok(())
}

async fn serve(listener: TcpListener, store: Store) {
    loop {
        let Ok((stream, _)) = listener.accept().await else { continue };
        let store = Arc::clone(&store);
        tokio::spawn(async move {
            let _ = handle(stream, store).await;
        });
    }
}
