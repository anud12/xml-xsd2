/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string} = hostApi.runtime;

  // "first" is created first: the old first-entity fallback would have
  // marked it whenever a filter did not match. The data-driven evaluator
  // must not.
  hostApi.runtime.setEntity({
    id: string.of("first"),
    textMap: { flag: string.of("no") }
  });
  hostApi.runtime.setEntity({
    id: string.of("second"),
    textMap: { flag: string.of("no") }
  });

  // Exact id match via the filters leaf.
  const exactMark = hostApi.runtime.setEffect({
    name: string.of("mark-exact"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
       ctx.getMutableEntityBy({
         filters: [(e) => e.id.isContainingExactly(string.of("second"))],
       })
         .map(ee => ee.text_map.put("flag", () => hostApi.runtime.maybe.of(string.of("exact"))))
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
         .map(ee => ee.text_map.put("flag", () => hostApi.runtime.maybe.of(string.of("contains"))))
    }
  });

  hostApi.runtime.emitEvent(exactMark, {});
  hostApi.runtime.emitEvent(containsMark, {});
};
