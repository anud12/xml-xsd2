pub(super) fn host_api_script_panel() -> &'static str {
    r#"registerPanel(p) {
        try {
            var toPush = p;
            if (p && typeof p === 'object') {
                toPush = JSON.stringify(p);
            } else if (typeof p === 'string') {
                toPush = JSON.stringify({ id: p });
            } else {
                toPush = JSON.stringify({ id: String(p) });
            }
            globalThis.__registeredPanels =
                globalThis.__registeredPanels || [];
            globalThis.__registeredPanels.push(toPush);
        } catch(e) { /* ignore */ }
    },"#
}

pub(super) fn host_api_script_create_entity()
    -> &'static str
{
    r#"createEntity(obj) {
        globalThis.__createdEntities =
            globalThis.__createdEntities || [];
        try {
            if (obj && typeof obj === 'object'
                && typeof obj.firstName === 'string') {
                globalThis.__createdEntities.push(
                    { firstName: obj.firstName });
                globalThis.__logs =
                    globalThis.__logs || [];
                globalThis.__logs.push(
                    `entity created: ${obj.firstName}`);
            } else {
                globalThis.__createdEntities.push(obj);
                globalThis.__logs =
                    globalThis.__logs || [];
                globalThis.__logs.push(
                    `entity created: ${String(obj)}`);
            }
        } catch(e) {
            globalThis.__createdEntities.push(
                String(obj));
            globalThis.__logs =
                globalThis.__logs || [];
            globalThis.__logs.push(
                `entity created: ${String(obj)}`);
        }
    },"#
}

pub(super) fn host_api_script_set_entity()
    -> &'static str
{
    r#"setEntity(id, data) {
        globalThis.__entityData =
            globalThis.__entityData || {};
        if (typeof id === 'string' && data
            && typeof data === 'object') {
            globalThis.__entityData[id] = data;
        }
    },"#
}

pub(super) fn host_api_script_register_entity()
    -> &'static str
{
    r#"registerEntity(obj) {
        globalThis.__entityData =
            globalThis.__entityData || {};
        try {
            if (obj && typeof obj === 'object') {
                var id = obj.id || String(obj);
                globalThis.__entityData[id] = obj;
            }
        } catch(e) { /* ignore */ }
    },"#
}

pub(super) fn host_api_script_register_container()
    -> &'static str
{
    r#"registerContainer(c) {
        try {
            var toPush = c;
            if (c && typeof c === 'object') {
                toPush = JSON.stringify(
                    globalThis.serializeContainer(c));
            }
            globalThis.__registeredContainers =
                globalThis.__registeredContainers || [];
            globalThis.__registeredContainers.push(toPush);
            globalThis.__logs = globalThis.__logs || [];
            globalThis.__logs.push('container registered: ' + (c.id || c));
        } catch(e) {
            globalThis.__logs = globalThis.__logs || [];
            globalThis.__logs.push('ERROR registerContainer: ' + e);
        }
    },"#
}
