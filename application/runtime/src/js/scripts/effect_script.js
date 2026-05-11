(function(){
  var evs = globalThis.__registeredEvents || [];
  var f = null;
  for (var i = 0; i < evs.length; i++) {
    if (evs[i] && typeof evs[i] === 'object' && evs[i].name === EFFECT_NAME_PLACEHOLDER) {
      f = evs[i];
      break;
    }
  }
  if (f) {
    if (typeof f.prepare === 'function') { try { f.prepare({}); } catch(ex) {} }
    if (typeof f.apply === 'function') { try { f.apply(buildEffectContext()); } catch(e) {} }
  }
})();
