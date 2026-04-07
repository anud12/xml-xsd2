use std::collections::HashMap;
use std::fs::File;
use std::io::Read;
use std::path::Path;

/// Reads all files from a zip archive into a name→content map.
pub fn read_zip_files(zip_path: &str) -> HashMap<String, String> {
    let file = match File::open(zip_path) {
        Ok(f) => f,
        Err(_) => return HashMap::new(),
    };
    let mut archive = match zip::ZipArchive::new(file) {
        Ok(a) => a,
        Err(_) => return HashMap::new(),
    };
    let mut files = HashMap::new();
    for i in 0..archive.len() {
        let mut f = match archive.by_index(i) {
            Ok(f) => f,
            Err(_) => continue,
        };
        let name = f.name().to_string();
        println!("loaded {}", name);
        let mut raw: Vec<u8> = Vec::new();
        use std::io::Read as _;
        f.read_to_end(&mut raw).unwrap_or_default();
        let contents = String::from_utf8_lossy(&raw).to_string();
        println!("{} loaded", name);
        files.insert(name, contents);
    }
    files
}

/// Creates a valid empty zip at `zip_path` if the path is non-empty and the file doesn't exist.
pub fn create_empty_zip_if_missing(zip_path: &str) {
    if !zip_path.is_empty() && !Path::new(zip_path).exists() {
        if let Ok(file) = File::create(zip_path) {
            let writer = zip::ZipWriter::new(file);
            let _ = writer.finish();
        }
    }
}
