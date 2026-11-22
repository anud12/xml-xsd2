/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string} = hostApi.runtime;

  // Both IDs intentionally include "sec" for the substring predicate.
  hostApi.runtime.setEntity({
    id: string.of("first"),
    stringMap: { flag: string.of("no") },
  });
  hostApi.runtime.setEntity({
    id: string.of("second"),
    stringMap: { flag: string.of("no"), filter_key: string.of("second") },
  });

  // Exact id match via the filters leaf.
  const exactMark = hostApi.runtime.setEffect({
    name: string.of("mark-exact"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
       ctx.getMutableEntityBy({
         filters: [(e) => e.id.isContainingExactly(string.of("second"))],
       })
         .map(ee => ee.string_map.put("flag", () => hostApi.runtime.maybe.of(string.of("exact"))))
    }
  });

  // Exact stringMap match, extracted through the list-expression API.
  const stringMapMark = hostApi.runtime.setEffect({
    name: string.of("mark-string-map"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
      ctx.getMutableEntityBy({
        filters: [(e) => e.string_map.get("filter_key")
          .map(t => t.isContainingExactly(string.of("second")))
          .orElse(hostApi.runtime.condition.of(false))],
      }).exactlyOne()
        .map(ee => ee.string_map.put("flag", () => hostApi.runtime.maybe.of(string.of("exact"))));
    }
  });

  // Substring id match (the plain-string input shape is gone).
  const containsMark = hostApi.runtime.setEffect({
    name: string.of("mark-contains"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
       ctx.getMutableEntityBy({
         filters: [(e) => e.id.isContaining(string.of("sec"))],
       })
         .forEach(ee => ee.string_map.put("substring_match", () => hostApi.runtime.maybe.of(string.of("contains"))))
    }
  });

  hostApi.runtime.emitEvent(exactMark, {});
  hostApi.runtime.emitEvent(stringMapMark, {});
  hostApi.runtime.emitEvent(containsMark, {});
};
