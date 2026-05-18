(function() {
    __clearAstNodes();
    var actionName = ACTION_NAME_PLACEHOLDER;
    var acts = globalThis.__registeredActions || [];
    var action = null;
    for (var i = 0; i < acts.length; i++) {
        if (typeof acts[i] === 'string') {
            if (acts[i] === actionName) { action = { name: acts[i], apply: function(){} }; break; }
        } else if (acts[i] && typeof acts[i] === 'object') {
            if (acts[i].name === actionName) { action = acts[i]; break; }
        }
    }
    if (!action || !action.apply || typeof action.apply !== 'function') {
        return __flushAstNodes();
    }
    var iContext = createInstrumentedEventContext(createInstrumentedHostApi());
    // Call apply function directly with context as argument (NOT using .apply() method)
    try { action.apply(iContext); } catch(e) {}
    // Fallback: try calling without arguments for no-param closures
    if (Object.keys(__astNodes).length === 0) {
        __clearAstNodes();
        try { action.apply(); } catch(e) {}
    }
    return __flushAstNodes();
})();
