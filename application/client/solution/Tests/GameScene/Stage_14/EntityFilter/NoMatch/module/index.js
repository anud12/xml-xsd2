/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string, number, condition} = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: string.of("x"),
    textMap: { flag: string.of("no"), job: string.of("smith") }
  });
  hostApi.runtime.setEntity({
    id: string.of("y"),
    textMap: { flag: string.of("no"), job: string.of("healer") }
  });

  const noMatch = {
    filters: [
      (e) => e.text_map.get(string.of("job"))
        .map(t => t.isContainingExactly(string.of("nobody")))
        .orElse(condition.of(false)),
    ],
  };

  const mark = hostApi.runtime.setEffect({
    name: string.of("mark-no-match"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
      // Direct map: the callback must never run.
      ctx.getMutableEntityBy(noMatch)
        .map(ee => {
          ee.text_map.get(string.of("flag")).ifPresent(v => {
            v.map(string.of("marked"));
          });
          return number.of(0);
        });
      // Degraded element access: the element callback must never run.
      ctx.getMutableEntityBy(noMatch).get(number.of(0))
        .map(v => {
          v.text_map.get(string.of("flag")).ifPresent(t => {
            t.map(string.of("marked"));
          });
          return number.of(0);
        });
    }
  });

  hostApi.runtime.emitEvent(mark, {});
};
