pub fn host_api_script_log() -> &'static str {
    r#"log(msg) {
        try {
            globalThis.__logs =
                globalThis.__logs || [];
            globalThis.__logs.push(String(msg));
        } catch(e) { }
    }, number: (function() {
        function numOf(o) { return Number(typeof o === 'object' && o !== null ? (o._raw !== undefined ? o._raw : o.value) : o); }
        function wrap(x) {
            x = Number(x);
            return {
                _raw: x,
                value: x,
                of: function(y) { return wrap(y); },
                sum: function(o) { return wrap(x + numOf(o)); },
                subtract: function(o) { return wrap(x - numOf(o)); },
                multiply: function(o) { return wrap(x * numOf(o)); },
                divide: function(o) { return wrap(x / numOf(o)); },
                modulo: function(o) { return wrap(x % numOf(o)); },
                toString: function() { return String(x); },
                toJSON: function() { return x; },
                isGreaterThan: function(o) { var t = numOf(o); return globalThis.condition.of(x > t); },
                isLessThan: function(o) { var t = numOf(o); return globalThis.condition.of(x < t); },
                isGreaterOrEqualTo: function(o) { var t = numOf(o); return globalThis.condition.of(x >= t); },
                isLessOrEqualTo: function(o) { var t = numOf(o); return globalThis.condition.of(x <= t); },
                isEqualTo: function(o) { var t = numOf(o); return globalThis.condition.of(x === t); },
                isNotEqualTo: function(o) { var t = numOf(o); return globalThis.condition.of(x !== t); },
                toStringExpression: function() { return globalThis.host.string.of(String(x)); }
            };
        }
        // number.random draws from the shared seeded LXM stream
        // (globalThis.__lxmNext), so it is governed by the module seed.
        return {
            of: function(n) { return wrap(Number(n)); },
            random: function(from, to) { var f = Math.floor(numOf(from)); var t = Math.floor(numOf(to)); var r = f + Math.floor(globalThis.__lxmNext() * (t - f + 1)); return wrap(r); }
        };
    })(),
    string: { of: function(s) {
        var v = String(s);
        return {
            _raw: v,
            value: v,
            of: function(x) { return globalThis.host.string.of(x); },
            map: function(x) { return globalThis.host.string.of(typeof x === 'object' ? x._raw || x.value : x); },
            concat: function(o) { return globalThis.host.string.of(v + String(typeof o === 'object' ? o._raw || o.value : o)); },
            toString: function() { return v; },
            toJSON: function() { return v; },
            isContaining: function(o) { var t = String(typeof o === 'object' ? o._raw || o.value : o); return globalThis.condition.of(v.indexOf(t) >= 0); },
            isContainingExactly: function(o) { var t = String(typeof o === 'object' ? o._raw || o.value : o); return globalThis.condition.of(v === t); }
        };
    } },
    texture: {
        of: function(t) { return t; }
    },"#
}

pub fn host_api_script_animation() -> &'static str {
    r#"setAnimation(name, args) {
        globalThis.__registeredAnimations =
            globalThis.__registeredAnimations || {};
        globalThis.__animationHandles =
            globalThis.__animationHandles || {};
        var resolvedName = typeof name === 'object' ? name.value : name;
        if (typeof resolvedName === 'string') {
            var handle = {
                name: resolvedName,
                value: resolvedName,
                toString: function() { return resolvedName; },
                toJSON: function() { return resolvedName; }
            };
            globalThis.__registeredAnimations[resolvedName] = args;
            globalThis.__animationHandles[resolvedName] = handle;
            return handle;
        }
    },
    getAnimation(name) {
        var resolvedName = typeof name === 'object' ? name.value : name;
        if (globalThis.__animationHandles
            && globalThis.__animationHandles[resolvedName]) {
            return globalThis.__animationHandles[resolvedName];
        }
        return null;
    },"#
}

fn host_api_script_behavior() -> &'static str {
    super::script_behavior::host_api_script_behavior()
}

fn host_api_script_condition() -> &'static str {
    r#"condition:{
        of:function(v){
          return{
            value:v,
            ifTrue:function(cb){if(v&&typeof cb==='function')cb();},
            ifFalse:function(cb){if(!v&&typeof cb==='function')cb();}
          };
        }
      },"#
}

pub fn host_api_script_rest() -> String {
    use super::script_register::host_api_script_register_block;
    use super::script_panel_entity::{
        host_api_script_panel,
        host_api_script_create_entity,
        host_api_script_set_entity,
    };
    let mut parts: Vec<String> = Vec::new();
    parts.push(
        host_api_script_register_block("setEvent"));
    parts.push(
        host_api_script_register_block("setAction"));
    parts.push(
        host_api_script_register_block("setEffect"));
    parts.push(host_api_script_panel().to_string());
    parts.push(
        host_api_script_create_entity().to_string());
        parts.push(
        host_api_script_set_entity().to_string());
    parts.push(host_api_script_behavior().to_string());
    parts.push(host_api_script_condition().to_string());
    parts.push(host_api_script_log().to_string());
    parts.push(host_api_script_animation().to_string());
    let mut s = parts.join("");
    s.push_str(" }");
    s
}

pub fn host_api_script_tail() -> String {
    host_api_script_convenience().to_string()
}

fn host_api_script_convenience() -> &'static str {
    r#"globalThis.createEntity = function(o) {
        return globalThis.host.createEntity(o);
    };
    globalThis.entity = globalThis.entity || {};
    globalThis.entity.create = function(o) {
        return globalThis.host.createEntity(o);
    };
    function string_of(s) { return s; }
"#
}
