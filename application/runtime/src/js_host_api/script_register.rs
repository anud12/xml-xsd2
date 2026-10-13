use super::script_emit::get_scan_fn;

pub(super) fn host_api_script_register_block(
    kind: &str,
) -> String {
    let label = if kind == "setEvent" { "Events" }
        else if kind == "setAction" { "Actions" }
        else { "Events" };
    let mut s = String::new();
    s.push_str(kind);
    s.push_str("(ev) { let n = 'unknown'; ");
    s.push_str("if (ev && typeof ev === 'object') { ");
    s.push_str("if (typeof ev.name === 'string') n = ev.name; ");
    s.push_str("else if (ev.apply && typeof ev.apply === ");
    s.push_str("'function' && ev.apply.name) ");
    s.push_str("n = ev.apply.name; ");
    s.push_str("} else if (typeof ev === 'string') { ");
    s.push_str("n = ev; } ");
    s.push_str("globalThis.__logs = ");
    s.push_str("globalThis.__logs || []; ");
    s.push_str("globalThis.__logs.push(`");
    s.push_str(label);
    s.push_str(" set: ${n}`); ");
    s.push_str(&format!(
        "globalThis.__registered{} = \
         globalThis.__registered{} || []; \
         var __existing = -1; \
         for (var __i = 0; __i < globalThis.__registered{}.length; __i++) {{ \
           var __e = globalThis.__registered{}[__i]; \
           var __en = (typeof __e === 'string') ? __e \
             : (typeof __e === 'object' && __e !== null) \
                 ? ((typeof __e.name === 'string') ? __e.name \
                    : (__e.apply && typeof __e.apply === 'function' ? __e.apply.name : null)) \
                 : null; \
           if (__en === n) {{ __existing = __i; break; }} \
         }} \
         if (__existing >= 0) {{ globalThis.__registered{}[__existing] = ev; }} \
         else {{ globalThis.__registered{}.push(ev); }}",
        label, label, label, label, label, label
    ));
    s.push_str("try { ");
    s.push_str(get_scan_fn());
    s.push_str(" let owner = n; ");
    s.push_str("scanFn(ev.prepare, owner); ");
    s.push_str("scanFn(ev.apply, owner); ");
    s.push_str("} catch(e) { /* ignore */ } ");
    s.push_str("return { name: n, value: n, toString: function() { return n; }, toJSON: function() { return n; } }; ");
    s.push_str("},");
    s
}
