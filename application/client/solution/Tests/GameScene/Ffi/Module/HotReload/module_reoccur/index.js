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
      context.getEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("node-1"))],
      })
        .map(elementExpr => {
          elementExpr.number_map.get(string.of("key")).ifPresent(v => {
            v.sum(number.of(1));
          });
        })
    }
  });

  hostApi.runtime.emitEvent(repeat, {});
}
