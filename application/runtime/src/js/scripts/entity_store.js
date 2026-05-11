// Build entity store from __entityData
globalThis.__entityStore = [];
if (globalThis.__entityData) {
  for (var id in globalThis.__entityData) {
    var e = JSON.parse(JSON.stringify(globalThis.__entityData[id]));
    e.textMap_name = id;
    globalThis.__entityStore.push(e);
  }
}
