use cucumber::{given, when, then, World};
use std::fs;
use std::path::Path;
use std::collections::HashMap;
use std::io::Write;

#[derive(Debug, Default, cucumber::World)]
struct TestWorld {
    archive: Option<Vec<u8>>,
    files: Option<HashMap<String, Vec<u8>>>,
}

#[given(expr = "the test directory contains files")]
async fn test_directory_contains_files(world: &mut TestWorld) {
    let dir = "./src";
    let mut files = HashMap::new();
    for entry in fs::read_dir(dir).unwrap() {
        let entry = entry.unwrap();
        let path = entry.path();
        if path.is_file() {
            let name = path.file_name().unwrap().to_string_lossy().to_string();
            let content = fs::read(&path).unwrap();
            files.insert(name, content);
        }
    }
    world.files = Some(files);
}

#[when(expr = "I create an archive of all files in the directory")]
async fn create_archive(world: &mut TestWorld) {
    use std::io::Cursor;
    let files = world.files.as_ref().unwrap();
    let mut zip = zip::ZipWriter::new(Cursor::new(Vec::new()));
    let options = zip::write::FileOptions::default();
    for (name, content) in files.iter() {
        zip.start_file(name, options).unwrap();
        zip.write_all(content).unwrap();
    }
    let archive = zip.finish().unwrap().into_inner();
    world.archive = Some(archive);
}

#[then(expr = "the archive should contain all files with correct contents")]
async fn archive_should_contain_files(world: &mut TestWorld) {
    use std::io::Cursor;
    let archive = world.archive.as_ref().unwrap();
    let files = world.files.as_ref().unwrap();
    let mut reader = zip::ZipArchive::new(Cursor::new(archive)).unwrap();
    assert_eq!(reader.len(), files.len());
    for i in 0..reader.len() {
        let mut file = reader.by_index(i).unwrap();
        let mut content = Vec::new();
        std::io::Read::read_to_end(&mut file, &mut content).unwrap();
        let name = file.name().to_string();
        assert_eq!(content, files.get(&name).unwrap().as_slice());
    }
}


fn main() {
    futures::executor::block_on(async {
        TestWorld::run("./features").await;
    });
}






