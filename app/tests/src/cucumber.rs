use cucumber::{World, Cucumber};
pub // Use archive_steps from crate root
// Use archive_steps from crate root
include!("../archive_steps.rs");

#[tokio::main]
pub async fn main() {
    Cucumber::<archive_steps::ArchiveWorld>::new()
        .features(&["./suite/archive/archive.feature"])
        .steps(archive_steps::steps())
        .run_and_exit()
        .await;
}
