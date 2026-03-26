use std::fs::File;
use std::io::{self, Write, Read, Seek, SeekFrom};
use std::path::Path;
use zip::write::FileOptions;
use zip::ZipWriter;

pub fn createZipArchive<P: AsRef<Path>>(dir: P, archive_path: P, files: &[&str]) -> io::Result<()> {
    let archive_file = File::create(&archive_path)?;
    let mut zip = ZipWriter::new(archive_file);
    let options = FileOptions::default().compression_method(zip::CompressionMethod::Stored);
    for &file_name in files {
        let file_path = dir.as_ref().join(file_name);
        let mut f = File::open(&file_path)?;
        let mut buffer = Vec::new();
        f.read_to_end(&mut buffer)?;
        zip.start_file(file_name, options)?;
        zip.write_all(&buffer)?;
    }
    zip.finish()?;
    Ok(())
}

pub fn listZipFiles<P: AsRef<Path>>(archive_path: P) -> io::Result<Vec<String>> {
    let archive_file = File::open(archive_path)?;
    let mut archive = zip::ZipArchive::new(archive_file)?;
    let mut names = Vec::new();
    for i in 0..archive.len() {
        let file = archive.by_index(i)?;
        names.push(file.name().to_string());
    }
    Ok(names)
}
