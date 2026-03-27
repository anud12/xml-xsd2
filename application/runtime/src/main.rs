use std::env;
use std::fs::File;
use std::io::Read;
use std::path::Path;
use anyhow::{Context, Result};
use zip::ZipArchive;

fn main() -> Result<()> {
    let args: Vec<String> = env::args().collect();
    let zipPath = args.get(1).context("Missing path to zip file argument")?;
    let file = File::open(&zipPath).context("Failed to open zip file")?;
    let mut archive = ZipArchive::new(file).context("Failed to read zip archive")?;

    for i in 0..archive.len() {
        let mut file = archive.by_index(i).context("Failed to access file in archive")?;
        println!("{}", file.name());
        let mut fileContent= Default::default();
        file.read_to_string(&mut fileContent)?;
        println!("{}", fileContent);
    }
    Ok(())
}
