//! Minimal helpers to create a rquickjs Runtime and Context.
//!
//! This module exposes small, well-typed functions used to bootstrap an
//! embedded QuickJS runtime. It intentionally returns the Runtime by value
//! (so the caller owns it) and provides a helper to create a Context tied to
//! that Runtime. Keeping ownership explicit avoids self-referential types.

use anyhow::Result;
use rquickjs::{Context, Runtime};

/// Create a new QuickJS Runtime.
///
/// The runtime owns the VM and can be passed around. To obtain an execution
/// context use `create_context(&runtime)`; the context borrows from the
/// runtime and therefore must not outlive it.
pub fn create_runtime() -> Result<Runtime> {
    let rt = Runtime::new()?;
    rt.set_loader(
        crate::js_executor::ArchiveResolver,
        crate::js_executor::ArchiveLoader,
    );
    Ok(rt)
}

/// Create a Context bound to an existing Runtime.
///
/// Returns a Context that borrows from the provided Runtime. Typical usage:
///
/// let rt = create_runtime()?;
/// let ctx = create_context(&rt)?;
/// // use ctx within this scope
pub fn create_context(runtime: &Runtime) -> Result<Context> {
    let ctx = Context::full(runtime)?;
    Ok(ctx)
}
