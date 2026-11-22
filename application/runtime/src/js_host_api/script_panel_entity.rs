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

pub(super) fn host_api_script_set_entity() -> &'static str {
    r#"setEntity(arg) {
        var id = arg && globalThis.__entityId(arg.id);
        if (!id) return;
        var data = {
            id: id,
            numberMap: Object.assign(Object.create(null), arg.numberMap),
            stringMap: Object.assign(Object.create(null), arg.stringMap),
            polygons: Object.assign(Object.create(null), arg.polygons)
        };
        if (arg.entityListMap) {
            data.entityListMap = Object.create(null);
            for (var name of Object.keys(arg.entityListMap)) {
                data.entityListMap[name] = Array.from(new Set(
                    arg.entityListMap[name].map(globalThis.__entityId).filter(Boolean)));
            }
        }
        // Normalize scalar expressions, but do not serialize behavior closures.
        for (var key of Object.keys(data.numberMap)) {
            var value = Number(data.numberMap[key]);
            if (Number.isFinite(value)) data.numberMap[key] = value;
            else delete data.numberMap[key];
        }
        for (var key of Object.keys(data.stringMap)) {
            data.stringMap[key] = String(data.stringMap[key]);
        }
        globalThis.__writeEntity(data);
        if (arg.behavior !== undefined) {
            var b = arg.behavior;
            var bVal = typeof b === 'object' ? b : { name: b };
            globalThis.__behaviors = globalThis.__behaviors || {};
            globalThis.__behaviors[id] = bVal;
            globalThis.__logs = globalThis.__logs || [];
            globalThis.__logs.push('behavior attached: ' + id + ' -> ' + (bVal ? bVal.name : ''));
        }
        if (arg.sector && typeof arg.sector === 'object') {
            var sectors = globalThis.__registeredSectors || (globalThis.__registeredSectors = []);
            var index = sectors.findIndex(function (row) { return JSON.parse(row).id === id; });
            var row = JSON.stringify({ id: id, sector: arg.sector });
            if (index < 0) sectors.push(row);
            else sectors[index] = row;
        }
        return globalThis.__entityProxy(id);
    },"#
}
