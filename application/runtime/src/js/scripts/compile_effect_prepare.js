(function() {
    __clearAstNodes();
    var effectName = EFFECT_NAME_PLACEHOLDER;
    var evs = globalThis.__registeredEvents || [];
    var effect = null;
    for (var i = 0; i < evs.length; i++) {
        if (typeof evs[i] === 'string') {
            if (evs[i] === effectName) { effect = { name: evs[i], prepare: function(){} }; break; }
        } else if (evs[i] && typeof evs[i] === 'object') {
            if (evs[i].name === effectName) { effect = evs[i]; break; }
        }
    }
    if (!effect || !effect.prepare || typeof effect.prepare !== 'function') {
        return __flushAstNodes();
    }
    var iHostApi = createInstrumentedHostApi();
    // Call prepare function - it returns payload but we don't need it for compilation
    try { effect.prepare(); } catch(e) {}
    return __flushAstNodes();
})();
