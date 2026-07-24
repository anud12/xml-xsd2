namespace NewGameProject.Module;

static class HostApiSetup
{
    internal const string Script = @"
var __registeredEntities = {};
var __registeredContainers = {};
var __registeredAnimations = {};
var hostApi = {
    ui: {
        texture: {
            of: function(p) { return p; }
        },
        getSpritePNG: function(p) { return p; },
        getAnimation: function(name, animationDuration) {
            var resolvedName = typeof name === ""object"" ? name.value : name;
            if (__registeredAnimations[resolvedName]) {
                return __registeredAnimations[resolvedName];
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
            if (typeof resolvedId === ""string"") __registeredEntities[resolvedId] = data;
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
        container: {}
    }
};
";
}
