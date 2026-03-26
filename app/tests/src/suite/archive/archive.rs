use std::fs::File;
use std::io::{self, Write, Read};
use std::path::Path;
use zip::write::FileOptions;
use zip::ZipWriter;

#[allow(non_snake_case)]
pub fn createZipArchive(dir: &Path, archivePath: &Path, files: &[&str]) -> io::Result<()> {
    let archiveFile = File::create(archivePath)?;
    let mut zip = ZipWriter::new(archiveFile);
    let options: FileOptions<'_, ()> = FileOptions::default().compression_method(zip::CompressionMethod::Stored);
    for &fileName in files {
        let filePath = dir.join(fileName);
        let mut f = File::open(&filePath)?;
        let mut buffer = Vec::new();
        f.read_to_end(&mut buffer)?;
        zip.start_file(fileName, options)?;
        zip.write_all(&buffer)?;
    }
    zip.finish()?;
    Ok(())
}

#[allow(non_snake_case)]
pub fn listZipFiles(archivePath: &Path) -> io::Result<Vec<String>> {
    let archiveFile = File::open(archivePath)?;
    let mut archive = zip::ZipArchive::new(archiveFile)?;
    let mut names = Vec::new();
    for i in 0..archive.len() {
        let file = archive.by_index(i)?;
        names.push(file.name().to_string());
    }
    Ok(names)
}
