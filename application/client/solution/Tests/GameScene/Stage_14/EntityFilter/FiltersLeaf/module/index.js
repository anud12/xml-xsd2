/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string, number, condition} = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: string.of("e-smith"),
    textMap: { flag: string.of("no"), job: string.of("smith") },
    numberMap: { hp: number.of(3) }
  });
  hostApi.runtime.setEntity({
    id: string.of("e-smith2"),
    textMap: { flag: string.of("no"), job: string.of("smith") },
    numberMap: { hp: number.of(8) }
  });
  hostApi.runtime.setEntity({
    id: string.of("e-healer"),
    textMap: { flag: string.of("no"), job: string.of("healer") },
    numberMap: { hp: number.of(3) }
  });

  // Leaf: every lambda must hold. Only e-smith satisfies both.
  const leaf = {
    filters: [
      (e) => e.text_map.get(string.of("job"))
        .map(t => t.isContainingExactly(string.of("smith")))
        .orElse(condition.of(false)),
      (e) => e.number_map.get(string.of("hp"))
        .map(n => n.isLessOrEqualTo(number.of(5)))
        .orElse(condition.of(false)),
    ],
  };
  const leafMark = hostApi.runtime.setEffect({
    name: string.of("mark-leaf"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
      ctx.getEntityBy(leaf)
        .map(ee => ee.text_map.get(string.of("flag")).ifPresent(v => {
          v.set(string.of("leaf"));
        }))
    }
  });

  // Number comparison surface: isEqualTo on the number expression.
  const eqMark = hostApi.runtime.setEffect({
    name: string.of("mark-eq"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
      ctx.getEntityBy({
        filters: [
          (e) => e.number_map.get(string.of("hp"))
            .map(n => n.isEqualTo(number.of(8)))
            .orElse(condition.of(false)),
        ],
      })
        .map(ee => ee.text_map.get(string.of("flag")).ifPresent(v => {
          v.set(string.of("eq"));
        }))
    }
  });

  hostApi.runtime.emitEvent(leafMark, {});
  hostApi.runtime.emitEvent(eqMark, {});
};
