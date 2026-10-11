pub(super) fn host_api_script_panel() -> &'static str {
    r#"registerPanel: function() { /* no-op — handled by C# */ },"#
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
    r#"setEntity(arg) {
        globalThis.__entityData =
            globalThis.__entityData || {};
        if (!arg || typeof arg !== 'object') return;
        var idRaw = arg.id;
        var id = (idRaw && typeof idRaw === 'object')
            ? idRaw.value : idRaw;
        if (typeof id !== 'string' || id === '') return;
        globalThis.__entityData[id] = arg;
        if (arg.behavior !== undefined) {
            var b = arg.behavior;
            var bVal = typeof b === 'object' ? b : { name: b };
            globalThis.__behaviors =
                globalThis.__behaviors || {};
            globalThis.__behaviors[id] = bVal;
            globalThis.__logs = globalThis.__logs || [];
            globalThis.__logs.push('behavior attached: ' + id + ' -> ' + (bVal ? bVal.name : ''));
        }
    },"#
}

pub(super) fn host_api_script_set_container()
    -> &'static str
{
    r#"setContainer(arg) {
        try {
            if (!arg || typeof arg !== 'object') return;
            var idRaw = arg.id;
            var resolvedId = (idRaw && typeof idRaw === 'object')
                ? idRaw.value : idRaw;
            globalThis.__containerData =
                globalThis.__containerData || {};
            if (typeof resolvedId !== 'string' || resolvedId === '') return;
            arg.id = resolvedId;
            globalThis.__containerData[resolvedId] = arg;
            var data = arg;
            var out = {id: resolvedId};
            if (data.entities) {
                out.entities = data.entities.map(function(e) { return String(e); });
            }
            if (data.textMap) out.textMap = data.textMap;
            if (data.numberMap) out.numberMap = data.numberMap;
            if (data.getX) out.getX = globalThis.evalPositionFn ? globalThis.evalPositionFn(data.getX) : {};
            if (data.getY) out.getY = globalThis.evalPositionFn ? globalThis.evalPositionFn(data.getY) : {};
            if (data.getSpanX) out.getSpanX = globalThis.evalPositionFn ? globalThis.evalPositionFn(data.getSpanX) : {};
            if (data.getSpanY) out.getSpanY = globalThis.evalPositionFn ? globalThis.evalPositionFn(data.getSpanY) : {};
            if (data.sizeX) out.sizeX = data.sizeX;
            if (data.sizeY) out.sizeY = data.sizeY;
            if (data.alignment !== undefined) out.alignment = data.alignment;
            if (globalThis.__posKey) {
                if (data.getX) out.xKey = globalThis.__posKey(data.getX, "x");
                if (data.getY) out.yKey = globalThis.__posKey(data.getY, "y");
            }
            var serialized = JSON.stringify(out);
            globalThis.__registeredContainers =
                globalThis.__registeredContainers || [];
            // Upsert: replace any existing entry with the same id, else push.
            var __found = -1;
            for (var __i = 0; __i < globalThis.__registeredContainers.length; __i++) {
                try {
                    var __p = JSON.parse(globalThis.__registeredContainers[__i]);
                    if (__p && __p.id === resolvedId) { __found = __i; break; }
                } catch (e) { /* ignore */ }
            }
            if (__found >= 0) {
                globalThis.__registeredContainers[__found] = serialized;
            } else {
                globalThis.__registeredContainers.push(serialized);
            }
            // Optional sector attachment: a polyomino footprint on a named
            // grid. Captured verbatim (plain data) for the Rust sector
            // layer to normalize, place, and link against neighbours.
            if (data.sector && typeof data.sector === 'object') {
                globalThis.__registeredSectors =
                    globalThis.__registeredSectors || [];
                var __secFound = -1;
                for (var __s = 0; __s < globalThis.__registeredSectors.length; __s++) {
                    try {
                        var __sp = JSON.parse(globalThis.__registeredSectors[__s]);
                        if (__sp && __sp.id === resolvedId) { __secFound = __s; break; }
                    } catch (e) { /* ignore */ }
                }
                var __sec = JSON.stringify({ id: resolvedId, sector: data.sector });
                if (__secFound >= 0) {
                    globalThis.__registeredSectors[__secFound] = __sec;
                } else {
                    globalThis.__registeredSectors.push(__sec);
                }
            }
        } catch(e) { /* ignore */ }
    },"#
}
