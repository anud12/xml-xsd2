use cucumber::{given, then, when, World, Steps};
use std::fs::{self, File};
use std::io::Write;
use std::path::PathBuf;
use suite::archive::archive::{createZipArchive, listZipFiles};

#[derive(Debug, Default, World)]
pub struct ArchiveWorld {
    pub dir: Option<PathBuf>,
    pub archive_path: Option<PathBuf>,
    pub files: Vec<String>,
    pub archive_files: Vec<String>,
}

pub fn steps() -> Steps<ArchiveWorld> {
    let mut builder: Steps<ArchiveWorld> = Steps::new();

    builder.given("a directory with files \"file1.txt\" and \"file2.txt\"", |world, _step| {
        let dir = tempfile::tempdir().unwrap();
        let file1 = dir.path().join("file1.txt");
        let file2 = dir.path().join("file2.txt");
        File::create(&file1).unwrap().write_all(b"hello").unwrap();
        File::create(&file2).unwrap().write_all(b"world").unwrap();
        world.dir = Some(dir.into_path());
        world.files = vec!["file1.txt".to_string(), "file2.txt".to_string()];
        world
    });

    builder.when("I create an archive from the directory", |mut world, _step| {
        let dir = world.dir.as_ref().unwrap();
        let archive_path = dir.join("archive.zip");
        let file_names: Vec<&str> = world.files.iter().map(|s| s.as_str()).collect();
        createZipArchive(dir, &archive_path, &file_names).unwrap();
        world.archive_path = Some(archive_path);
        world
    });

    builder.then("the archive should contain \"file1.txt\" and \"file2.txt\"", |mut world, _step| {
        let archive_path = world.archive_path.as_ref().unwrap();
        let files = listZipFiles(archive_path).unwrap();
        assert!(files.contains(&"file1.txt".to_string()));
        assert!(files.contains(&"file2.txt".to_string()));
        world.archive_files = files;
        world
    });

    builder
}
