use std::env;
use std::fs::File;
use std::path::Path;
use anyhow::{Context, Result};
use zip::ZipArchive;

fn main() -> Result<()> {
    let args: Vec<String> = env::args().collect();
    let zipPath = args.get(1).context("Missing path to zip file argument")?;
    let file = File::open(&zipPath).context("Failed to open zip file")?;
    let mut archive = ZipArchive::new(file).context("Failed to read zip archive")?;

    for i in 0..archive.len() {
        let file = archive.by_index(i).context("Failed to access file in archive")?;
        println!("{}", file.name());
    }
    Ok(())
}
