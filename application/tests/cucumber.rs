use cucumber::{World, Cucumber};
mod archive_steps;

#[tokio::main]
async fn main() {
    Cucumber::<archive_steps::ArchiveWorld>::new()
        .features(&["./suite/archive/archive.feature"])
        .steps(archive_steps::steps())
        .run_and_exit()
        .await;
}
