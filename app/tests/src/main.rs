#![allow(non_snake_case)]

mod suite;
mod archive_steps;
use archive_steps::ArchiveWorld;
use cucumber::World;

#[tokio::main]
async fn main() {
    ArchiveWorld::cucumber()
        .run_and_exit("suite/archive/archive.feature")
        .await;
}

