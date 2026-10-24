/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: string.of("node-1"),
    numberMap: {
      "key": number.of(0)
    }
  });

  // Recurs every 3 units.
  const repeat = hostApi.runtime.setEffect({
    name: string.of("repeat"),
    reoccurAfterMs: (context, executionCount, input, output) => {
      return hostApi.runtime.maybe.of(number.of(3));
    },
    apply: (context, output) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("node-1"))],
      })
        .map(elementExpr => {
          const current = Number(elementExpr.number_map.get(string.of("key")).orElse(number.of(0)));
          elementExpr.number_map.put("key", number.of(current + 1));
        })
    }
  });

  hostApi.runtime.emitEvent(repeat, {});
}
