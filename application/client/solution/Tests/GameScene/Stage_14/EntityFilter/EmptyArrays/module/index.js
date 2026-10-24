/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string} = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: string.of("p"),
    textMap: { f1: string.of("no"), f2: string.of("no"), f3: string.of("no"), f4: string.of("no") }
  });
  hostApi.runtime.setEntity({
    id: string.of("q"),
    textMap: { f1: string.of("no"), f2: string.of("no"), f3: string.of("no"), f4: string.of("no") }
  });

  // Every empty variant must match everything (vacuous truth).
  const variants = [
    {filters: []},
    {and: []},
    {or: []},
    {not: []},
  ];

  variants.forEach((variant, i) => {
    const flag = "f" + (i + 1);
    const effect = hostApi.runtime.setEffect({
      name: string.of("mark-" + flag),
      prepare: () => ({value: true}),
      apply: (ctx) => {
        ctx.getMutableEntityBy(variant)
           .map(ee => ee.text_map.put(flag, string.of("yes")))
      }
    });
    hostApi.runtime.emitEvent(effect, {});
  });
};
