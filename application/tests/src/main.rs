use cucumber::{given, when, then, World};
#[derive(Debug, Default, cucumber::World)]
struct TestWorld;

#[given(expr = "a working system")]
async fn a_working_system(_world: &mut TestWorld) {
    // Setup code
}

#[when(expr = "I perform an action")]
async fn i_perform_action(_world: &mut TestWorld) {
    // Action code
}

#[then(expr = "I should see the expected result")]
async fn i_should_see_expected_result(_world: &mut TestWorld) {
    // Assertion code
}

fn main() {
    futures::executor::block_on(async {
        TestWorld::run("./features").await;
    });
}






