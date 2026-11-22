/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string, number, condition} = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: string.of("a"),
    numberMap: { hp: number.of(3) },
    stringMap: { flag: string.of("no"), job: string.of("smith"), team: string.of("red") },
  });
  hostApi.runtime.setEntity({
    id: string.of("b"),
    numberMap: { hp: number.of(9) },
    stringMap: { flag: string.of("no"), job: string.of("healer"), team: string.of("red") },
  });
  hostApi.runtime.setEntity({
    id: string.of("c"),
    numberMap: { hp: number.of(7) },
    stringMap: { flag: string.of("no"), job: string.of("smith"), team: string.of("blue") },
  });
  hostApi.runtime.setEntity({
    id: string.of("d"),
    numberMap: { hp: number.of(1) },
    stringMap: { flag: string.of("no"), job: string.of("ghost"), team: string.of("red") },
  });

  // Nested composition: not( or( blue-team, and( ghost, low-hp ) ) )
  // -> matches a and b only.
  const nested = {
    not: [
      {
        or: [
          {
            filters: [(e) => e.string_map.get(string.of("team"))
              .map(t => t.isContainingExactly(string.of("blue")))
              .orElse(condition.of(false))],
          },
          {
            and: [
              {
                filters: [(e) => e.string_map.get(string.of("job"))
                  .map(t => t.isContainingExactly(string.of("ghost")))
                  .orElse(condition.of(false))],
              },
              {
                filters: [(e) => e.number_map.get(string.of("hp"))
                  .map(n => n.isLessOrEqualTo(number.of(5)))
                  .orElse(condition.of(false))],
              },
            ],
          },
        ],
      },
    ],
  };
  const mark = hostApi.runtime.setEffect({
    name: string.of("mark-nested"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
      ctx.getMutableEntityBy(nested)
         .map(ee => ee.string_map.put("flag", () => hostApi.runtime.maybe.of(string.of("yes"))))
    }
  });

  hostApi.runtime.emitEvent(mark, {});
};
