#![allow(non_snake_case)]

#![allow(non_snake_case)]
#![allow(deprecated)]
use cucumber::{given, when, then, World};
use std::fs::File;
use std::io::Write;
use std::path::PathBuf;
use crate::suite::archive::archive::{createZipArchive, listZipFiles};

#[derive(Debug, Default, World)]
pub struct ArchiveWorld {
    pub dir: Option<PathBuf>,
    pub archivePath: Option<PathBuf>,
    pub files: Vec<String>,
    pub archiveFiles: Vec<String>,
}

#[given(expr = "a directory with files \"file1.txt\" and \"file2.txt\"")]
async fn given_directory_with_files(world: &mut ArchiveWorld) {
    let dir = tempfile::tempdir().unwrap();
    let file1 = dir.path().join("file1.txt");
    let file2 = dir.path().join("file2.txt");
    File::create(&file1).unwrap().write_all(b"hello").unwrap();
    File::create(&file2).unwrap().write_all(b"world").unwrap();
    world.dir = Some(dir.into_path());
    world.files = vec!["file1.txt".to_string(), "file2.txt".to_string()];
}

#[when(expr = "I create an archive from the directory")]
async fn when_create_archive(world: &mut ArchiveWorld) {
    let dir = world.dir.as_ref().unwrap();
    let archivePath = dir.join("archive.zip");
    let fileNames: Vec<&str> = world.files.iter().map(|s| s.as_str()).collect();
    createZipArchive(dir, &archivePath, &fileNames).unwrap();
    world.archivePath = Some(archivePath);
}

#[then(expr = "the archive should contain \"file1.txt\" and \"file2.txt\"")]
async fn then_archive_should_contain(world: &mut ArchiveWorld) {
    let archivePath = world.archivePath.as_ref().unwrap();
    let files = listZipFiles(archivePath).unwrap();
    assert!(files.contains(&"file1.txt".to_string()));
    assert!(files.contains(&"file2.txt".to_string()));
    world.archiveFiles = files;
}

