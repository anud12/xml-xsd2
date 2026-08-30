namespace NewGameProject.Module;

static class HostApiSetup
{
    internal const string Script = @"
var __registeredEntities = {};
var __registeredContainers = {};
var __registeredAnimations = {};
// Shared emitter for the merged panel builder. Surface options (x/y/width/
// height/background/onHover/onClick/anchor, or the forced flag from the
// window alias) mark the node as a positioned surface; layout marks it as a
// flow container. A node may declare both.
var __panelEmit = function(id, options, children, forceSurface) {
    var opts = options || {};
    var anchorMap = {
        'top-left': [0, 0], 'top': [0.5, 0], 'top-right': [1, 0],
        'left': [0, 0.5], 'center': [0.5, 0.5], 'right': [1, 0.5],
        'bottom-left': [0, 1], 'bottom': [0.5, 1], 'bottom-right': [1, 1]
    };
    var hasSurface = forceSurface
        || opts.x !== undefined || opts.y !== undefined
        || opts.width !== undefined || opts.height !== undefined
        || opts.background !== undefined || opts.onHover !== undefined
        || opts.onClick !== undefined || opts.anchor !== undefined;
    var json = { id: id };
    if (hasSurface) {
        var anchor = [0.5, 0.5];
        if (opts.anchor) {
            if (typeof opts.anchor === 'string' && anchorMap[opts.anchor]) {
                anchor = anchorMap[opts.anchor];
            } else if (typeof opts.anchor === 'object') {
                anchor = [opts.anchor.x !== undefined ? opts.anchor.x : 0.5,
                          opts.anchor.y !== undefined ? opts.anchor.y : 0.5];
            }
        }
        var background = opts.background;
        if (background && typeof background === ""object"" && typeof background.name === ""string"" && !background.frames) {
            var anim = __registeredAnimations[background.name];
            if (anim && anim.frames) {
                var resolved = {
                    name: background.name,
                    duration: background.duration !== undefined ? background.duration : (anim.duration !== undefined ? anim.duration : 1),
                    loop: background.loop !== undefined ? background.loop : (anim.loop !== undefined ? anim.loop : false),
                    frames: anim.frames
                };
                background = resolved;
            }
        }
        json.background = background;
        json.surface = true;
        json.size = { width: opts.width || 0, height: opts.height || 0 };
        json.anchor = { x: anchor[0], y: anchor[1] };
        json.offset = {
            top: opts.y || 0, bottom: 0,
            left: opts.x || 0,
            right: opts.width ? (opts.width - (opts.x || 0)) : 0
        };
        json.hover = opts.onHover ? {
            texture: opts.onHover.texture !== undefined ? opts.onHover.texture : null,
            thickness: (opts.onHover.thickness !== undefined ? opts.onHover.thickness : 0),
            background: opts.onHover.background !== undefined ? opts.onHover.background : null,
            emitAction: opts.onHover.emitAction || null,
            stopPropagation: opts.onHover.stopPropagation || false
        } : null;
        json.onClick = opts.onClick ? { type: ""emitAction"", actionName: opts.onClick } : null;
    }
    if (opts.layout !== undefined) {
        json.layout = typeof opts.layout === ""object"" ? opts.layout
            : (opts.layout === ""row"" ? { rowFirst: true } : { rowFirst: false });
    }
    json.children = children || [];
    __host_registerPanel(JSON.stringify(json));
    return id;
};
var hostApi = {
    ui: {
        getSpritePNG: function(p) { return p; },
        spriteMapTIFF: function(mapPath, layers) {
            if (typeof __host_fileExists === ""function"" && !__host_fileExists(mapPath)) {
                if (typeof __host_log === ""function"") __host_log(""Error: sprite map file not found: "" + mapPath);
            }
            var layerArr = [];
            for (var i = 0; i < layers.length; i++) {
                layerArr.push({
                    layer: layers[i].layer,
                    texture: layers[i].texture
                });
            }
            return { __spriteMap: true, map: mapPath, layers: layerArr };
        },
        getAnimation: function(name, animationDuration) {
            var resolvedName = typeof name === ""object"" ? name.value : name;
            if (__registeredAnimations[resolvedName]) {
                var result = {};
                for (var k in __registeredAnimations[resolvedName]) {
                    result[k] = __registeredAnimations[resolvedName][k];
                }
                if (animationDuration && animationDuration.duration) {
                    result.duration = animationDuration.duration;
                }
                if (animationDuration && animationDuration.loop !== undefined) {
                    result.loop = animationDuration.loop;
                }
                return result;
            }
            return null;
        },
        panel: function(id, options, children) {
            return __panelEmit(id, options, children, false);
        },
        window: function(id, options, children) {
            return __panelEmit(id, options, children, true);
        },
        text: function(id, value) {
            __host_registerPanel(JSON.stringify({
                id: id,
                content: { type: ""constant"", value: String(value) }
            }));
            return id;
        },
        field: function(id, binding) {
            var isNumber = binding && binding.map === ""number"";
            var content = {
                type: isNumber ? ""entityNumberValue"" : ""entityTextValue"",
                name: binding ? binding.name : """",
                entityId: binding ? binding.entity : """",
                fallback: binding && typeof binding.fallback === ""string"" ? binding.fallback : """"
            };
            if (binding && typeof binding.align === ""string"") content.align = binding.align;
            __host_registerPanel(JSON.stringify({
                id: id,
                content: content
            }));
            return id;
        },
        div: function(id, options, children) {
            var layout = options && options.layout ? options.layout : ""column"";
            return __panelEmit(id, { layout: layout }, children, false);
        },
        container: function(id, options, template) {
            var containerId = options && options.container;
            var resolvedContainerId = (typeof containerId === ""object"") ? containerId.value : containerId;
            var vertical = options && options.vertical !== undefined ? options.vertical : true;
            var entityIds = [];
            if (resolvedContainerId && __registeredContainers[resolvedContainerId]) {
                var ents = __registeredContainers[resolvedContainerId].entities;
                if (ents) {
                    for (var ei = 0; ei < ents.length; ei++) {
                        entityIds.push(typeof ents[ei] === ""object"" ? ents[ei].value : ents[ei]);
                    }
                }
            }
            var childIds = [];
            for (var i = 0; i < entityIds.length; i++) {
                var result = template({ id: entityIds[i], index: i });
                var arr = Array.isArray(result) ? result : [result];
                for (var k = 0; k < arr.length; k++) {
                    if (arr[k] != null) childIds.push(arr[k]);
                }
            }
                __host_registerPanel(JSON.stringify({
                    id: id,
                    content: { type: ""containerListView"", containerId: containerId, vertical: vertical },
                    children: childIds
                }));
                return id;
            }
        },
    runtime: {
        getEntityBy: function(filter) {
            var resolvedIds = [];
            if (filter && filter.__ids) resolvedIds = filter.__ids;
            else {
                for (var k in __registeredEntities) {
                    if (__registeredEntities.hasOwnProperty(k)) resolvedIds.push(k);
                }
            }
            var _makeNumberExpr = function(data, id, nResolved) {
                var _cv = null, _hv = false;
                if (data && data.numberMap && data.numberMap[nResolved] !== undefined) {
                    _cv = data.numberMap[nResolved];
                    if (_cv && typeof _cv === ""object"") _cv = _cv.value;
                    _hv = typeof _cv === ""number"";
                }
                var numberExpr = {
                    value: _cv,
                    isCondition: function(pred) {
                        var v = _hv ? _cv : 0;
                        var predVal = (pred && typeof pred === ""object"") ? pred.value : pred;
                        var result = (typeof predVal === ""function"") ? predVal(v) : true;
                        return {
                            getOnTrueOrFalse: function(trueVal, falseVal) {
                                var tv = (trueVal && typeof trueVal === ""object"") ? trueVal.value : trueVal;
                                var fv = (falseVal && typeof falseVal === ""object"") ? falseVal.value : falseVal;
                                return result ? tv : fv;
                            }
                        };
                    },
                    isLessOrEqualTo: function(limit) {
                        var lim = (typeof limit === ""object"") ? limit.value : limit;
                        return {
                            getOnTrueOrFalse: function(trueVal, falseVal) {
                                var tv = (trueVal && typeof trueVal === ""object"" && trueVal.value !== undefined) ? trueVal.value : trueVal;
                                var fv = (falseVal && typeof falseVal === ""object"" && falseVal.value !== undefined) ? falseVal.value : falseVal;
                                 return (_hv && _cv < lim) ? tv : fv;
                            }
                        };
                    },
                    map: function(fn2) {
                        if (_hv) fn2(numberExpr);
                        return numberExpr;
                    },
                    sum: function(amount) {
                        var a = (typeof amount === ""object"") ? amount.value : amount;
                        if (_hv) {
                            var nv = _cv + a;
                            _cv = nv;
                            numberExpr.value = nv;
                            data.numberMap[nResolved] = nv;
                            if (typeof __host_setEntityNumber === ""function"") __host_setEntityNumber(id, nResolved, nv);
                        }
                        return numberExpr;
                    },
                    modulo: function(m) {
                        var md = (typeof m === ""object"") ? m.value : m;
                        var result = _hv && md ? _cv % md : null;
                        return {
                            isEqualTo: function(target) {
                                var t = (typeof target === ""object"") ? target.value : target;
                                return __makeCondition(result !== null && result === t);
                            }
                        };
                    }
                };
                return numberExpr;
            };
            var __makeCondition = function(boolVal) {
                var c = {
                    __condition: boolVal === true
                };
                c.ifTrue = function(fn) { if (c.__condition && typeof fn === ""function"") fn(); return c; };
                c.ifFalse = function(fn) { if (!c.__condition && typeof fn === ""function"") fn(); return c; };
                c.orElse = function(other) {
                    var ov = (other && typeof other.__condition === ""boolean"") ? other.__condition : !!other;
                    c.__condition = c.__condition || ov;
                    return c;
                };
                return c;
            };
            var collection = {
                map: function(fn) {
                    for (var i = 0; i < resolvedIds.length; i++) {
                        var id = resolvedIds[i];
                        var data = __registeredEntities[id];
                        fn({
                            id: id,
                            getNumber: function(name) {
                                var nResolved = (typeof name === ""object"") ? name.value : name;
                                return _makeNumberExpr(data, id, nResolved);
                            },
                            getText: function(name) {
                                var nResolved = (typeof name === ""object"") ? name.value : name;
                                var _has = data && data.textMap && data.textMap[nResolved] !== undefined;
                                var _tv = null;
                                if (_has) {
                                    _tv = data.textMap[nResolved];
                                    if (_tv && typeof _tv === ""object"") _tv = _tv.value;
                                }
                                return {
                                    map: function(fn2) {
                                        if (_has) fn2(_tv);
                                        return collection;
                                    },
                                    ifPresent: function(fn2) {
                                        if (_has && typeof fn2 === ""function"") {
                                            fn2({
                                                set: function(v) {
                                                    var sv = (typeof v === ""object"") ? v.value : v;
                                                    data.textMap[nResolved] = sv;
                                                    if (typeof __host_setEntityText === ""function"")
                                                        __host_setEntityText(id, nResolved, sv);
                                                }
                                            });
                                        }
                                        return collection;
                                    }
                                };
                            }
                        });
                    }
                    return collection;
                },
                get: function(_count) {
                    return collection;
                },
                flatMap: function(fn) {
                    var _flatValues = [];
                    for (var i = 0; i < resolvedIds.length; i++) {
                        var id = resolvedIds[i];
                        var data = __registeredEntities[id];
                        fn({
                            id: id,
                            getNumber: function(name) {
                                var nResolved = (typeof name === ""object"") ? name.value : name;
                                var expr = _makeNumberExpr(data, id, nResolved);
                                _flatValues.push(expr);
                                return expr;
                            }
                        });
                    }
                    var flatColl = {
                        map: function(fn) {
                            var _mapped = [];
                            for (var j = 0; j < _flatValues.length; j++) {
                                _mapped.push(fn(_flatValues[j]));
                            }
                            return {
                                orElse: function(fallback) {
                                    for (var k = 0; k < _mapped.length; k++) {
                                        var mv = _mapped[k];
                                        if (mv && typeof mv.ifTrue === ""function"")
                                            return mv.orElse(fallback);
                                    }
                                    return (fallback && typeof fallback.ifTrue === ""function"")
                                        ? fallback : __makeCondition(false);
                                }
                            };
                        },
                        isCondition: function(pred) {
                            var _results = [];
                            for (var j = 0; j < _flatValues.length; j++) {
                                var r = pred(_flatValues[j]);
                                _results.push(r);
                            }
                            return {
                                getOnTrueOrFalse: function(trueVal, falseVal) {
                                    for (var k = 0; k < _results.length; k++) {
                                        var rv = _results[k];
                                        if (rv && typeof rv.getOnTrueOrFalse === ""function"") {
                                            return rv.getOnTrueOrFalse(trueVal, falseVal);
                                        }
                                    }
                                    return falseVal;
                                }
                            };
                        },
                        get: function(_c2) { return flatColl; },
                        flatMap: function(_fn2) { return flatColl; }
                    };
                    return flatColl;
                },
                isCondition: function(pred) {
                    var _condResults = [];
                    for (var i = 0; i < resolvedIds.length; i++) {
                        var id = resolvedIds[i];
                        var data = __registeredEntities[id];
                        var el = {
                            id: id,
                            getNumber: function(name) {
                                var nResolved = (typeof name === ""object"") ? name.value : name;
                                return _makeNumberExpr(data, id, nResolved);
                            }
                        };
                        _condResults.push(pred(el));
                    }
                    return {
                        getOnTrueOrFalse: function(trueVal, falseVal) {
                            for (var k = 0; k < _condResults.length; k++) {
                                var rv = _condResults[k];
                                if (rv && typeof rv.getOnTrueOrFalse === ""function"") {
                                    return rv.getOnTrueOrFalse(trueVal, falseVal);
                                }
                            }
                            return falseVal;
                        }
                    };
                }
            };
            return collection;
        },
        number: { of: function(n) { return n; } },
        string: { of: function(s) { return s; } },
        maybe: {
            of: function(v) { return v; },
            none: function() { return { __maybe: ""none"" }; }
        },
        setEntity: function(id, data) {
            var resolvedId = typeof id === ""object"" ? id.value : id;
            if (typeof resolvedId === ""string"" && data && typeof data === ""object"") {
                __registeredEntities[resolvedId] = data;
                if (data.behavior !== undefined) {
                    var bName = (typeof data.behavior === ""object"") ? data.behavior.name : data.behavior;
                    globalThis.__behaviors = globalThis.__behaviors || {};
                    globalThis.__behaviors[resolvedId] = { name: bName };
                    if (typeof __host_attachBehavior === ""function"")
                        __host_attachBehavior(resolvedId, String(bName));
                }
                var tm = data.textMap || {};
                for (var tk in tm) {
                    var tv = tm[tk];
                    if (tv && typeof tv === ""object"") tv = tv.value;
                    if (typeof tv === ""string"") __host_setEntityText(resolvedId, tk, tv);
                }
                var nm = data.numberMap || {};
                for (var nk in nm) {
                    var nv = nm[nk];
                    if (nv && typeof nv === ""object"") nv = nv.value;
                    if (typeof nv === ""number"") __host_setEntityNumber(resolvedId, nk, nv);
                }
            }
            return { name: id };
        },
        setContainer: function(id, data) {
            var resolvedId = typeof id === ""object"" ? id.value : id;
            if (typeof resolvedId === ""string"") __registeredContainers[resolvedId] = data;
        },
        registerEffect: function(args) {
            if (args && typeof args.apply === ""function"") {
                var name = (typeof args.name === ""object"") ? args.name.value : args.name;
                if (typeof __host_registerEffect === ""function"") {
                    var reoccur = args.reoccurAfterMs !== undefined ? args.reoccurAfterMs : null;
                    __host_registerEffect(String(name), reoccur, args.prepare, args.apply);
                }
            }
            return { name: args ? args.name : null };
        },
        registerAction: function(args) {
            if (args && typeof args.apply === ""function"") {
                var name = (typeof args.name === ""object"") ? args.name.value : args.name;
                if (typeof __host_registerAction === ""function"") {
                    __host_registerAction(String(name), args.apply);
                    globalThis.__registeredActions = globalThis.__registeredActions || [];
                    globalThis.__registeredActions.push({ name: String(name) });
                }
            }
            return { name: args ? args.name : null };
        },
        registerAnimation: function(name, args) {
            var resolvedName = typeof name === ""object"" ? name.value : name;
            if (typeof resolvedName === ""string"") {
                __registeredAnimations[resolvedName] = args;
            }
        },
        getAnimation: function(name, animationDuration) {
            var resolvedName = typeof name === ""object"" ? name.value : name;
            if (__registeredAnimations[resolvedName]) {
                return __registeredAnimations[resolvedName];
            }
            return null;
        },
        registerContainer: function(c) {
            if (c && typeof c === ""object"" && typeof c.id === ""string"") {
                __registeredContainers[c.id] = c;
            }
        },
        registerEntity: function(obj) {
            if (obj && typeof obj === ""object"" && typeof obj.id === ""string"") {
                __registeredEntities[obj.id] = obj;
            }
        },
        emitEvent: function(name, data) {
            var n = (typeof name === ""object"") ? name.value : name;
            if (typeof __host_emitEffectResult === ""function"")
                return __host_emitEffectResult(String(n), data || {});
            if (typeof __host_emitEffect === ""function"") __host_emitEffect(String(n), data || {});
            return null;
        },
        log: function(msg) {
            if (typeof __host_log === ""function"") __host_log(String(msg));
        },
        entity: {
            filter: {
                create: function() {
                    return {
                        byId: function(idFn) {
                            return {
                                isContainingExactly: function(target) {
                                    var tResolved = (typeof target === ""object"") ? target.value : target;
                                    return { __ids: [tResolved] };
                                }
                            };
                        }
                    };
                }
            }
        },
        condition: { of: function(v) { return v; } },
        temporal: {},
        numberMap: {},
        textMap: {},
        container: {},
        registerBehavior: function(definition) {
            if (!definition || typeof definition !== ""object"") {
                throw new Error(""behavior: missing definition"");
            }
            var resolvedName = typeof definition.name === ""object""
                ? definition.name.value : definition.name;
            if (typeof resolvedName !== ""string"" || resolvedName === """") {
                throw new Error(""behavior: missing name"");
            }
            globalThis.__behaviorDefinitions =
                globalThis.__behaviorDefinitions || {};
            if (resolvedName in globalThis.__behaviorDefinitions) {
                throw new Error(""behavior: duplicate name ""
                    + resolvedName);
            }
            function checkUtilityRule(rule, owner) {
                if (!rule || typeof rule !== ""object""
                    || typeof rule.label !== ""string"") {
                    throw new Error(""behavior: utility rule missing label in ""
                        + owner);
                }
                if (typeof rule.score !== ""function"") {
                    throw new Error(""behavior: "" + rule.label
                        + "" missing score in "" + owner);
                }
                if (typeof rule.do !== ""function"") {
                    throw new Error(""behavior: "" + rule.label
                        + "" missing do in "" + owner);
                }
                var doCtx = {
                    action: function(name, payload) {
                        return { action: name, payload: payload };
                    },
                    wait: function(duration) {
                        return { wait: duration };
                    }
                };
                var steps = rule.do(doCtx);
                if (!Array.isArray(steps)) {
                    throw new Error(""behavior: do must return a step array in ""
                        + owner);
                }
                var registered = globalThis.__registeredActions || [];
                for (var s = 0; s < steps.length; s++) {
                    var st = steps[s];
                    if (!st || typeof st !== ""object""
                        || (st.action === undefined && st.wait === undefined)) {
                        throw new Error(""behavior: invalid step in ""
                            + rule.label);
                    }
                    if (st.action !== undefined) {
                        var found = false;
                        for (var a = 0; a < registered.length; a++) {
                            var act = registered[a];
                            if (act && typeof act === ""object""
                                && act.name === st.action) {
                                found = true;
                                break;
                            }
                        }
                        if (!found) {
                            throw new Error(""behavior: action ""
                                + st.action + "" not registered in ""
                                + rule.label);
                        }
                    }
                }
                rule.steps = steps;
            }
            function checkPriorityRule(rule, owner) {
                if (!rule || typeof rule !== ""object"") {
                    throw new Error(""behavior: priority rule missing label in ""
                        + owner);
                }
                if (typeof rule.condition !== ""function"") {
                    throw new Error(""behavior: "" + rule.label
                        + "" missing condition in "" + owner);
                }
                if (!Array.isArray(rule.utility)
                    || rule.utility.length === 0) {
                    throw new Error(""behavior: "" + rule.label
                        + "" missing utility in "" + owner);
                }
                for (var u = 0; u < rule.utility.length; u++) {
                    checkUtilityRule(rule.utility[u], rule.label);
                }
            }
            if (Array.isArray(definition.priority)) {
                if (definition.priority.length === 0) {
                    throw new Error(""behavior: priority must be a non-empty array"");
                }
                for (var p = 0; p < definition.priority.length; p++) {
                    checkPriorityRule(definition.priority[p], ""priority"");
                }
            } else if (Array.isArray(definition.utility)) {
                if (definition.utility.length === 0) {
                    throw new Error(""behavior: utility must be a non-empty array"");
                }
                for (var u = 0; u < definition.utility.length; u++) {
                    checkUtilityRule(definition.utility[u], ""utility"");
                }
            } else {
                throw new Error(
                    ""behavior: definition must declare priority or utility"");
            }
            globalThis.__behaviorDefinitions[resolvedName] = definition;
            if (typeof __host_log === ""function"")
                __host_log(""behavior registered: "" + resolvedName);
            return { name: definition.name };
        }
    }
};

globalThis.__behavior_steps = function(behaviorName) {
    var defs = globalThis.__behaviorDefinitions || {};
    var def = defs[behaviorName];
    if (!def) return null;
    var candidates = [];
    if (Array.isArray(def.priority)) {
        for (var p = 0; p < def.priority.length; p++) {
            var pr = def.priority[p];
            if (!pr || typeof pr.condition !== ""function"") continue;
            var ok = true;
            try {
                var c = pr.condition();
                ok = !c || c === true || (c.__maybe === undefined && c !== false);
            } catch (e) { ok = false; }
            if (!ok) continue;
            if (Array.isArray(pr.utility)) {
                for (var u = 0; u < pr.utility.length; u++) {
                    candidates.push(pr.utility[u]);
                }
            }
        }
    } else if (Array.isArray(def.utility)) {
        for (var u2 = 0; u2 < def.utility.length; u2++) {
            candidates.push(def.utility[u2]);
        }
    }
    var best = null, bestScore = -Infinity;
    for (var b = 0; b < candidates.length; b++) {
        var rule = candidates[b];
        if (!rule || typeof rule.score !== ""function"") continue;
        var score = 0;
        try { score = rule.score(); } catch (e) { continue; }
        if (score > bestScore) { bestScore = score; best = rule; }
    }
    if (!best || !Array.isArray(best.steps)) return null;
    return best.steps;
};
";
}
