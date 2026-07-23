pub fn host_api_script_entity_filter() -> &'static str {
    "entity:{filter:{create:function(){return{byId:function(f){return{fn:f}}}}}},"
}

pub fn host_api_script_log() -> &'static str {
    r#"log(msg) {
        try {
            globalThis.__logs =
                globalThis.__logs || [];
            globalThis.__logs.push(String(msg));
        } catch(e) { }
    }, number: { of: function(n) { return n; } },
    string: { of: function(s) { return s; } },
    texture: {
        of: function(t) { return t; },
        getAnimation: function(name) {
            var resolvedName = typeof name === 'object' ? name.value : name;
            if (globalThis.__registeredAnimations
                && globalThis.__registeredAnimations[resolvedName]) {
                return globalThis.__registeredAnimations[resolvedName];
            }
            return null;
        }
    },"#
}

pub fn host_api_script_animation() -> &'static str {
    r#"registerAnimation(name, args) {
        globalThis.__registeredAnimations =
            globalThis.__registeredAnimations || {};
        var resolvedName = typeof name === 'object' ? name.value : name;
        if (typeof resolvedName === 'string') {
            globalThis.__registeredAnimations[resolvedName] = args;
        }
    },
    getAnimation(name) {
        var resolvedName = typeof name === 'object' ? name.value : name;
        if (globalThis.__registeredAnimations
            && globalThis.__registeredAnimations[resolvedName]) {
            return globalThis.__registeredAnimations[resolvedName];
        }
        return null;
    },"#
}

pub fn host_api_script_rest() -> String {
    use super::script_register::host_api_script_register_block;
    use super::script_panel_entity::{
        host_api_script_panel,
        host_api_script_create_entity,
        host_api_script_set_entity,
        host_api_script_set_container,
        host_api_script_register_entity,
        host_api_script_register_container,
    };
    let mut parts: Vec<String> = Vec::new();
    parts.push(
        host_api_script_register_block("registerEvent"));
    parts.push(
        host_api_script_register_block("registerAction"));
    parts.push(
        host_api_script_register_block("registerEffect"));
    parts.push(host_api_script_panel().to_string());
    parts.push(
        host_api_script_create_entity().to_string());
        parts.push(
        host_api_script_set_entity().to_string());
    parts.push(
        host_api_script_set_container().to_string());
    parts.push(
        host_api_script_register_entity().to_string());
    parts.push(
        host_api_script_register_container().to_string());
    parts.push(host_api_script_entity_filter().to_string());
    parts.push(host_api_script_log().to_string());
    parts.push(host_api_script_animation().to_string());
    let mut s = parts.join("");
    s.push_str(" }");
    s
}

pub fn host_api_script_tail() -> String {
    format!(
        "{}{}{}{}",
        host_api_script_convenience(),
        host_api_script_serialize_container(),
        host_api_script_eval_position_fn(),
        host_api_script_make_entity_proxy()
    )
}

fn host_api_script_convenience() -> &'static str {
    r#"globalThis.createEntity = function(o) {
        return globalThis.host.createEntity(o);
    };
    globalThis.entity = globalThis.entity || {};
    globalThis.entity.create = function(o) {
        return globalThis.host.createEntity(o);
    };
    function string_of(s) { return s; }"#
}

fn host_api_script_serialize_container() -> &'static str {
    r#"
    globalThis.serializeContainer = function(c) {
        var out = {};
        if (c.id !== undefined) out.id = c.id;
        if (c.textMap !== undefined) out.textMap = c.textMap;
        if (c.numberMap !== undefined) {
            out.numberMap = {};
            for (var k in c.numberMap)
                out.numberMap[k] = c.numberMap[k];
        }
        if (c.entities !== undefined)
            out.entities = c.entities.map(function(e) {
                return String(e);
            });
        if (c.getX !== undefined)
            out.getX = globalThis.evalPositionFn(c.getX);
        if (c.getY !== undefined)
            out.getY = globalThis.evalPositionFn(c.getY);
        if (c.getSpanX !== undefined)
            out.getSpanX = globalThis.evalPositionFn(c.getSpanX);
        if (c.getSpanY !== undefined)
            out.getSpanY = globalThis.evalPositionFn(c.getSpanY);
        if (c.sizeX !== undefined)
            out.sizeX = {value: c.sizeX.value,
                outOfBounds: c.sizeX.outOfBounds};
        if (c.sizeY !== undefined)
            out.sizeY = {value: c.sizeY.value,
                outOfBounds: c.sizeY.outOfBounds};
        return out;
    };"#
}

fn host_api_script_eval_position_fn() -> &'static str {
    r#"
    globalThis.evalPositionFn = function(fn) {
        var result = {};
        var ids = Object.keys(globalThis.__entityData || {});
        for (var i = 0; i < ids.length; i++) {
            try {
                var ent = globalThis.makeEntityProxy(ids[i]);
                result[ids[i]] = fn(ent);
            } catch(e) { /* skip */ }
        }
        return result;
    };"#
}

fn host_api_script_make_entity_proxy() -> &'static str {
    r#"
    globalThis.makeEntityProxy = function(id) {
        var data = globalThis.__entityData[id] || {};
        var nm = data.numberMap || {};
        var tm = data.textMap || {};
        return {
            id: id,
            number_map: {
                get: function(key) {
                    return {
                        orElse: function(def) {
                            return nm.hasOwnProperty(key) ? nm[key] : def;
                        }
                    };
                }
            },
            text_map: {
                get: function(key) { return tm[key] || ''; }
            },
            getNumber: function(key) { return nm[key]; },
            getText: function(key) { return tm[key]; }
        };
    };"#
}
