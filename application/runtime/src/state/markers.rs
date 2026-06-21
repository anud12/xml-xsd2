use std::sync::atomic::Ordering;
use std::sync::Mutex;

pub fn mark_persisted_has_data() {
    super::persisted_flag().store(true, Ordering::SeqCst);
}

pub fn last_archive_path() -> &'static Mutex<String> {
    super::persisted_flag();
    unsafe { super::LAST_ARCHIVE_PATH.expect("archive path initialized") }
}

pub fn set_archive_path(path: &str) {
    *last_archive_path().lock().unwrap() = path.to_string();
}
