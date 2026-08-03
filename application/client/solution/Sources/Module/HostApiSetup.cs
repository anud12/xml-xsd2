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
        /// Create a sprite-map frame from a 16-bit integer TIFF mask and 8-bit PNG skin textures.
        /// The TIFF must be 16-bit unsigned integer per channel (RGBA), little-endian, uncompressed.
        /// The PNG skins must be standard 8-bit RGBA.
        /// R/G channels encode UV coordinates (0..mapSize-1), B selects skin index, A is mask alpha.
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
