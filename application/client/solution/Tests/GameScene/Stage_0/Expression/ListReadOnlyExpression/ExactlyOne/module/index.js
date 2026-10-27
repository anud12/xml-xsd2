/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // A lone entity with a known cost (the "exactly one" case).
  hostApi.runtime.setEntity({
    id: string.of("solo"),
    numberMap: { cost: number.of(5) }
  });

  // Two siblings sharing a substring (the "more than one" case).
  hostApi.runtime.setEntity({
    id: string.of("sib1"),
    numberMap: { cost: number.of(1) }
  });
  hostApi.runtime.setEntity({
    id: string.of("sib2"),
    numberMap: { cost: number.of(2) }
  });

  // A sink entity that receives the three extracted results.
  hostApi.runtime.setEntity({
    id: string.of("game"),
    numberMap: { one: number.of(0), zero: number.of(0), multi: number.of(0) }
  });

  const probe = hostApi.runtime.setEffect({
    name: string.of("probe-exactly-one"),
    prepare: () => ({ value: true }),
    apply: (ctx) => {
      const def = number.of(-1);
      // Exactly one -> present: carries "solo"'s cost (5).
      const one = ctx.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("solo"))],
      })
        .exactlyOne()
        .map((ee) => ee.number_map.get("cost").orElse(def))
        .orElse(def);
      // Zero -> absent: falls back to the default (-1).
      const zero = ctx.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("missing"))],
      })
        .exactlyOne()
        .map((ee) => ee.number_map.get("cost").orElse(def))
        .orElse(def);
      // More than one -> absent: falls back to the default (-1).
      const multi = ctx.getMutableEntityBy({
        filters: [(e) => e.id.isContaining(string.of("sib"))],
      })
        .exactlyOne()
        .map((ee) => ee.number_map.get("cost").orElse(def))
        .orElse(def);

      ctx.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).map((game) => {
        game.number_map.put("one", () => hostApi.runtime.maybe.of(one));
        game.number_map.put("zero", () => hostApi.runtime.maybe.of(zero));
        game.number_map.put("multi", () => hostApi.runtime.maybe.of(multi));
      });
    }
  });

  hostApi.runtime.emitEvent(probe, {});
};
