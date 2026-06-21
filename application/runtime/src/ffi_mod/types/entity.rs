use libc::{c_char, c_void};

/// FFI row for an entity change notification.
#[repr(C)]
pub struct EntityRow {
    pub text_map_name: *mut c_char,
}

pub type EntityChangeCb = extern "C" fn(*const EntityRow, *mut c_void);

/// Active subscription to entity change events.
#[repr(C)]
pub struct Subscription {
    pub id: *mut c_char,
    pub cb: Option<EntityChangeCb>,
    pub user_data: *mut c_void,
}

pub type UnsubscribeCb = extern "C" fn(*mut c_void);

/// Handle returned to callers for unsubscribing.
#[repr(C)]
pub struct UnsubscribeHandle {
    pub unsub: Option<UnsubscribeCb>,
    pub user_data: *mut c_void,
}

static SUBS_INIT: std::sync::Once = std::sync::Once::new();
static mut ENTITY_SUBSCRIPTIONS: Option<
    &'static std::sync::Mutex<Vec<*mut Subscription>>,
> = None;

#[allow(dead_code)]
pub(super) static SUB_ID_COUNTER: std::sync::atomic::AtomicUsize =
    std::sync::atomic::AtomicUsize::new(1);

/// Ensure the entity subscription vector is initialized.
pub fn ensure_entity_subscriptions() {
    SUBS_INIT.call_once(|| {
        let v = Box::leak(Box::new(std::sync::Mutex::new(Vec::new())));
        unsafe { ENTITY_SUBSCRIPTIONS = Some(v); }
    });
}

/// Get the entity subscription vector.
pub fn entity_subscriptions() -> &'static std::sync::Mutex<Vec<*mut Subscription>> {
    ensure_entity_subscriptions();
    unsafe { ENTITY_SUBSCRIPTIONS.expect("entity subs initialized") }
}
