namespace NewGameProject.Module;

static class HostApiSetup
{
    internal const string Script = @"
var __registeredEntities = {};
var __registeredContainers = {};
var __registeredAnimations = {};
var hostApi = {
    ui: {
        getSpritePNG: function(p) { return p; },
        spriteMapTIFF: function(mapPath, layers) {
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
        registerPanel: function(p) {
            if (p.content && p.content.type === ""containerListView"" && typeof p.content.template === ""function"") {
                var containerId = p.content.containerId;
                var vertical = p.content.vertical !== undefined ? p.content.vertical : true;
                var templateResults = [];
                var entityIds = __host_getContainerEntityIds(containerId);
                for (var i = 0; i < entityIds.length; i++) {
                    var entityId = entityIds[i];
                    var result = p.content.template({ getId: function(e) { return function() { return e; }; }(entityId) }, i);
                    templateResults.push(JSON.stringify(result));
                }
                var contentCopy = {};
                for (var k in p.content) {
                    if (k !== ""template"") contentCopy[k] = p.content[k];
                }
                contentCopy.__templateResults = templateResults;
                var panelCopy = {};
                for (var key in p) {
                    if (key === ""content"") panelCopy[key] = contentCopy;
                    else panelCopy[key] = p[key];
                }
                __host_registerPanel(JSON.stringify(panelCopy));
            }
            else {
                __host_registerPanel(JSON.stringify(p));
            }
        }
    },
    runtime: {
        number: { of: function(n) { return n; } },
        string: { of: function(s) { return s; } },
        setEntity: function(id, data) {
            var resolvedId = typeof id === ""object"" ? id.value : id;
            if (typeof resolvedId === ""string"" && data && typeof data === ""object"") {
                __registeredEntities[resolvedId] = data;
                if (data.behavior !== undefined) {
                    globalThis.__behaviors = globalThis.__behaviors || {};
                    globalThis.__behaviors[resolvedId] = { name: data.behavior };
                }
            }
            return { name: id };
        },
        setContainer: function(id, data) {
            var resolvedId = typeof id === ""object"" ? id.value : id;
            if (typeof resolvedId === ""string"") __registeredContainers[resolvedId] = data;
        },
        registerEffect: function() {},
        registerAction: function() {},
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
        emitEvent: function() {},
        log: function() {},
        entity: {
            filter: { create: function() { return { byId: function() { return {}; } }; } }
        },
        maybe: {
            of: function(v) { return v; },
            none: function() { return {}; }
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
                    throw new Error(""behavior: "" + rule.label
                        + "" do must return a step array in "" + owner);
                }
                var registered = globalThis.__registeredActions || [];
                for (var s = 0; s < steps.length; s++) {
                    var st = steps[s];
                    if (!st || typeof st !== ""object""
                        || (st.action === undefined && st.wait === undefined)) {
                        throw new Error(
                            ""behavior: invalid step in "" + rule.label);
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
                if (!rule || typeof rule !== ""object""
                    || typeof rule.label !== ""string"") {
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
                        + "" utility must be a non-empty array in ""
                        + owner);
                }
                for (var u = 0; u < rule.utility.length; u++) {
                    checkUtilityRule(rule.utility[u], rule.label);
                }
            }
            if (Array.isArray(definition.priority)) {
                if (definition.priority.length === 0) {
                    throw new Error(
                        ""behavior: priority must be a non-empty array"");
                }
                for (var p = 0; p < definition.priority.length; p++) {
                    checkPriorityRule(definition.priority[p], ""priority"");
                }
            } else if (Array.isArray(definition.utility)) {
                if (definition.utility.length === 0) {
                    throw new Error(
                        ""behavior: utility must be a non-empty array"");
                }
                for (var u = 0; u < definition.utility.length; u++) {
                    checkUtilityRule(definition.utility[u], ""utility"");
                }
            } else {
                throw new Error(
                    ""behavior: definition must declare priority or utility"");
            }
            globalThis.__behaviorDefinitions[resolvedName] = definition;
            return { name: definition.name };
        }
    }
};
";
}
