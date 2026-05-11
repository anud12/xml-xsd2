// Sync __entityStore mutations back to __entityData
if (globalThis.__entityData && globalThis.__entityStore) {
  for (var k in globalThis.__entityData) {
    for (var i = 0; i < globalThis.__entityStore.length; i++) {
      var entry = globalThis.__entityStore[i];
      if (entry && entry.textMap_name === k) {
        globalThis.__entityData[k] = JSON.parse(JSON.stringify(entry));
      }
    }
  }
}
