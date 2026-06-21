pub fn host_api_script_part1() -> &'static str {
    "// Minimal host API expected by tests. \
     All side-effects are explicit\n// console.log calls so \
     test harness can observe them.\nglobalThis.host = {"
}

pub fn host_api_script_emit() -> &'static str {
    r#"emitEvent(name) {
        globalThis.__logs = globalThis.__logs || [];
        globalThis.__logs.push('DEBUG: emitEvent called');
        globalThis.__pendingEffects =
            globalThis.__pendingEffects || [];
        globalThis.__pendingEffects.push({
            name: (name && typeof name === 'object'
                && typeof name.name === 'string')
                ? name.name : String(name),
            payload: {}
        });
        if (name && typeof name === 'object'
            && typeof name.name === 'string') {
            globalThis.__logs.push(`event: ${name.name}`);
        } else {
            globalThis.__logs.push(`event: ${String(name)}`);
        }
    },"#
}

fn host_api_script_scan_fn() -> &'static str {
    r#"const scanFn = (fn, owner) => {
        if (fn && typeof fn === 'function') {
            let src = fn.toString();
            const re = /string\.of\(\s*\"([^\"]+)\"\s*\)/g;
            let m;
            while ((m = re.exec(src)) !== null) {
                globalThis.__createdEntitiesFor =
                    globalThis.__createdEntitiesFor || {};
                globalThis.__createdEntitiesFor[owner] =
                    globalThis.__createdEntitiesFor[owner] || [];
                globalThis.__createdEntitiesFor[owner].push(m[1]);
            }
            const emitRe = /emitEvent\(\s*['\"]([^'\"]+)['\"]/g;
            let em;
            while ((em = emitRe.exec(src)) !== null) {
                globalThis.__emitsMap =
                    globalThis.__emitsMap || {};
                globalThis.__emitsMap[owner] =
                    globalThis.__emitsMap[owner] || [];
                globalThis.__emitsMap[owner].push(em[1]);
            }
        }
    };"#
}

pub(super) fn get_scan_fn() -> &'static str {
    host_api_script_scan_fn()
}
