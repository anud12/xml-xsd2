// Extraction and debug simulation use an isolated declaration map. Once the
// persistent context starts running, all reads/writes target Rust's entity map.
globalThis.__entityMap = Object.create(null);

globalThis.__entityId = function (ref) {
  if (typeof ref === "string") return ref;
  if (ref && typeof ref.value === "string") return ref.value;
  if (ref && typeof ref.id === "string") return ref.id;
  return undefined;
};

globalThis.__readEntity = function (id) {
  return globalThis.__nativeEntityRead
    ? JSON.parse(globalThis.__nativeEntityRead(id)) : globalThis.__entityMap[id];
};

globalThis.__entityIds = function () {
  return globalThis.__nativeEntityIds
    ? JSON.parse(globalThis.__nativeEntityIds()) : Object.keys(globalThis.__entityMap);
};

globalThis.__writeEntity = function (entity) {
  if (globalThis.__nativeEntityDeclare) globalThis.__nativeEntityDeclare(JSON.stringify(entity));
  else globalThis.__entityMap[entity.id] = entity;
};

globalThis.__writeEntityField = function (id, map, key, value) {
  if (globalThis.__nativeEntityField) {
    globalThis.__nativeEntityField(id, map, key, JSON.stringify(value) || "null");
  } else {
    const entity = globalThis.__entityMap[id];
    if (!entity) return;
    if (value === undefined) delete entity[map][key];
    else entity[map][key] = value;
  }
};

globalThis.__changeMembership = function (id, list, ref, add) {
  const member = globalThis.__entityId(ref);
  if (!member) return;
  if (globalThis.__nativeMembership) {
    globalThis.__nativeMembership(id, list, member, add);
  } else {
    const group = globalThis.__entityMap[id];
    const ids = group && group.entityListMap && group.entityListMap[list];
    if (!ids) return;
    const pos = ids.indexOf(member);
    if (add && pos < 0) ids.push(member);
    if (!add && pos >= 0) ids.splice(pos, 1);
  }
};
