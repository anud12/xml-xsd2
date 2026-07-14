namespace NewGameProject.Module;

static class HostApiSetup
{
    internal const string Script = @"
var hostApi = {
    number: { of: function(n) { return n; } },
    string: { of: function(s) { return s; } },
    texture: { of: function(p) { return p; } },
    registerPanel: function(p) {
        __host_registerPanel(JSON.stringify(p));
    },
    setEntity: function() {},
    setContainer: function() {},
    registerEffect: function() {},
    registerAction: function() {},
    registerContainer: function() {},
    registerEntity: function() {},
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
};
";
}
