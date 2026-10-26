use std::collections::HashMap;
use std::fs::{self, File, OpenOptions};
use std::io::{self, Read, Write};
use std::path::{Path, PathBuf};

const SET: u8 = 1;
const DELETE: u8 = 2;
const HEADER: usize = 9;

struct Store {
    path: PathBuf,
    file: File,
    index: HashMap<String, String>,
}

fn invalid(message: &str) -> io::Error {
    io::Error::new(io::ErrorKind::InvalidData, message.to_string())
}

fn encode(op: u8, key: &str, value: &str) -> Vec<u8> {
    let mut record = Vec::with_capacity(HEADER + key.len() + value.len());
    record.push(op);
    record.extend_from_slice(&(key.len() as u32).to_be_bytes());
    record.extend_from_slice(&(value.len() as u32).to_be_bytes());
    record.extend_from_slice(key.as_bytes());
    record.extend_from_slice(value.as_bytes());
    record
}

/// Replays the log into an index and returns the length of the valid prefix.
fn replay(bytes: &[u8], index: &mut HashMap<String, String>) -> io::Result<usize> {
    let mut offset = 0;
    while offset < bytes.len() {
        let Some(header) = bytes.get(offset..offset + HEADER) else {
            return Err(invalid("truncated record")); // Treats a crash-torn tail as corruption.
        };
        let op = header[0];
        let key_len = u32::from_be_bytes(header[1..5].try_into().unwrap()) as usize;
        let value_len = u32::from_be_bytes(header[5..9].try_into().unwrap()) as usize;
        let Some(end) = (offset + HEADER).checked_add(key_len).and_then(|n| n.checked_add(value_len)) else { break };
        let Some(body) = bytes.get(offset + HEADER..end) else { return Err(invalid("truncated record")) };
        let key = std::str::from_utf8(&body[..key_len]).map_err(|_| invalid("key is not UTF-8"))?;
        let value = std::str::from_utf8(&body[key_len..]).map_err(|_| invalid("value is not UTF-8"))?;
        match op {
            SET => {
                index.insert(key.to_string(), value.to_string());
            }
            DELETE => {
                index.remove(key);
            }
            _ => return Err(invalid("unknown record type")),
        }
        offset = end;
    }
    Ok(offset)
}

impl Store {
    fn open(path: &Path) -> io::Result<Store> {
        let mut file = OpenOptions::new().read(true).append(true).create(true).open(path)?;
        let mut bytes = Vec::new();
        file.read_to_end(&mut bytes)?;
        let mut index = HashMap::new();
        let valid = replay(&bytes, &mut index)?;
        if valid < bytes.len() {
            // A crash left a partial record at the end: cut it off so new records follow valid data.
            file.set_len(valid as u64)?;
            file.sync_all()?;
        }
        Ok(Store { path: path.to_path_buf(), file, index })
    }

    fn append(&mut self, record: &[u8]) -> io::Result<()> {
        self.file.write_all(record)?;
        self.file.sync_data()
    }

    fn set(&mut self, key: &str, value: &str) -> io::Result<()> {
        self.append(&encode(SET, key, value))?;
        self.index.insert(key.to_string(), value.to_string());
        Ok(())
    }

    fn delete(&mut self, key: &str) -> io::Result<bool> {
        if !self.index.contains_key(key) {
            return Ok(false);
        }
        self.append(&encode(DELETE, key, ""))?;
        self.index.remove(key);
        Ok(true)
    }

    fn get(&self, key: &str) -> Option<&str> {
        self.index.get(key).map(String::as_str)
    }

    fn len(&self) -> usize {
        self.index.len()
    }

    fn compact(&mut self) -> io::Result<()> {
        let temporary = self.path.with_extension("tmp");
        let mut keys: Vec<&String> = self.index.keys().collect();
        keys.sort();
        let mut output = File::create(&temporary)?;
        for key in keys {
            output.write_all(&encode(SET, key, &self.index[key]))?;
        }
        output.sync_all()?;
        drop(output);
        fs::rename(&temporary, &self.path)?;
        self.file = OpenOptions::new().read(true).append(true).open(&self.path)?;
        Ok(())
    }
}
