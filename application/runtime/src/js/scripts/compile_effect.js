(function() {
    __clearAstNodes();
    var effectName = EFFECT_NAME_PLACEHOLDER;
    var evs = globalThis.__registeredEvents || [];
    var effect = null;
    for (var i = 0; i < evs.length; i++) {
        if (typeof evs[i] === 'string') {
            if (evs[i] === effectName) { effect = { name: evs[i], apply: function(){} }; break; }
        } else if (evs[i] && typeof evs[i] === 'object') {
            if (evs[i].name === effectName) { effect = evs[i]; break; }
        }
    }
    if (!effect || !effect.apply || typeof effect.apply !== 'function') {
        return __flushAstNodes();
    }
    var iContext = createInstrumentedEventContext(createInstrumentedHostApi());
    // Call apply function directly with context as argument (NOT using .apply() method)
    try { effect.apply(iContext); } catch(e) {}
    return __flushAstNodes();
})();
