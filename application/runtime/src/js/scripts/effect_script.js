(function(){
  var evs = globalThis.__registeredEvents || [];
  var f = null;
  for (var i = 0; i < evs.length; i++) {
    if (evs[i] && typeof evs[i] === 'object' && evs[i].name === EFFECT_NAME_PLACEHOLDER) {
      f = evs[i];
      break;
    }
  }
  globalThis.__lastEffectReoccurAfterMs = null;
  if (f) {
    if (typeof f.prepare === 'function') { try { f.prepare({}); } catch(ex) {} }
    if (typeof f.apply === 'function') { try { f.apply(buildEffectContext()); } catch(e) {} }
    // Evaluate reoccurAfterMs to determine next scheduled execution
    if (typeof f.reoccurAfterMs === 'function') {
      try {
        var reoccurResult = f.reoccurAfterMs({}, 1, {}, {});
        // Extract numeric value from maybe.of(number.of(x)) pattern
        // reoccurResult structure: { isSome: true, value: { value: x } } or { isSome: false }
        if (reoccurResult && reoccurResult.isSome && reoccurResult.value) {
          var numVal = reoccurResult.value;
          if (typeof numVal === 'number') {
            globalThis.__lastEffectReoccurAfterMs = numVal;
          } else if (numVal && typeof numVal.value === 'number') {
            globalThis.__lastEffectReoccurAfterMs = numVal.value;
          }
        }
      } catch(e) {}
    }
  }
})();
