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
        // LXM PRNG: LCG + Xorshift + MurmurHash3 mixer
        var _lxm_state = 0x7FFFFFFF; // 2147483647 (Mersenne prime M31)
        function lxm_next() {
            _lxm_state = (Math.imul(_lxm_state, 1664525) + 1013904223) & 0xFFFFFFFF;
            var x = _lxm_state;
            x ^= x << 13; x &= 0xFFFFFFFF;
            x ^= x >>> 17;
            x ^= x << 5; x &= 0xFFFFFFFF;
            x ^= x >>> 16;
            x = Math.imul(x, 0x85ebca6b);
            x ^= x >>> 13;
            x = Math.imul(x, 0xc2b2ae35);
            x ^= x >>> 16;
            return (x >>> 0) / 4294967296;
        }
        return {
            of: function(n) { return wrap(Number(n)); },
            random: function(from, to) { var f = Math.floor(numOf(from)); var t = Math.floor(numOf(to)); var r = f + Math.floor(lxm_next() * (t - f + 1)); return wrap(r); }
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
        host_api_script_set_container,
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
    parts.push(
        host_api_script_set_container().to_string());
    parts.push(host_api_script_behavior().to_string());
    parts.push(host_api_script_condition().to_string());
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
    function string_of(s) { return s; }
    globalThis.__posKey = function(fn, def) {
        try {
            var s = Function.prototype.toString.call(fn);
            var m = s.match(/number_map\.get\(["']([A-Za-z0-9_\-]+)["']\)/);
            if (m) { return m[1]; }
        } catch (e) {}
        return def;
    };
    globalThis.__posMapKeys = function(m, def) {
        if (m && typeof m === 'object') {
            for (var k in m) {
                if (Object.prototype.hasOwnProperty.call(m, k)) { return k; }
            }
        }
        return def;
    };"#
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
        if (c.alignment !== undefined) out.alignment = c.alignment;
        if (globalThis.__posKey) {
            if (c.getX) out.xKey = globalThis.__posKey(c.getX, "x");
            if (c.getY) out.yKey = globalThis.__posKey(c.getY, "y");
        }
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
