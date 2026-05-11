(globalThis.__logs||[]).push("DEBUG: testing direct mutation for effect " + "EFFECT_NAME_PLACEHOLDER");
if (globalThis.__entityStore && globalThis.__entityStore.length > 0) {
  var e = globalThis.__entityStore[0];
  if (e && e.numberMap) {
    e.numberMap.value = (e.numberMap.value || 0) + 1;
    (globalThis.__logs||[]).push("DEBUG: mutated entity " + e.textMap_name + " value=" + e.numberMap.value);
  }
}
