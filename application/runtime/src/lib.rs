pub mod js_runtime;
pub mod js_host_api;
pub mod js_executor;

// Re-export commonly used types for tests and external callers
pub use js_host_api::Declarations;
pub use js_executor::extract_from_source;
