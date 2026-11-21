/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string, maybe } = hostApi.runtime;

  // Three entities with distinct ids and known numeric values (the list the
  // randomElement must pick from).
  hostApi.runtime.setEntity({ id: string.of("ra1"), numberMap: { v: number.of(1) } });
  hostApi.runtime.setEntity({ id: string.of("ra2"), numberMap: { v: number.of(2) } });
  hostApi.runtime.setEntity({ id: string.of("ra3"), numberMap: { v: number.of(3) } });

  // A sink entity that receives the picked value.
  hostApi.runtime.setEntity({ id: string.of("sink"), numberMap: { pick: number.of(-1) } });

  const probe = hostApi.runtime.setEffect({
    name: string.of("probe-list-random-element"),
    prepare: () => ({ value: true }),
    apply: (ctx) => {
      const def = number.of(-1);
      // A value list of the entities' numeric values, then randomElement() —
      // the value-list randomElement drawn from the shared seeded LXM stream.
      const pick = ctx.getMutableEntityBy({
        filters: [(e) => e.id.isContaining(string.of("ra"))],
      })
        .map((e) => e.number_map.get("v").orElse(def))
        .randomElement()
        .orElse(def);

      ctx.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("sink"))],
      }).forEach((s) => {
        s.number_map.put("pick", () => maybe.of(pick));
      });
    }
  });

  hostApi.runtime.emitEvent(probe, {});
};
