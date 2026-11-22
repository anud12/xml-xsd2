/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: string.of("item-1"),
    numberMap: { cost: number.of(5) },
  });

  hostApi.runtime.setEntity({
    id: string.of("item-2"),
    numberMap: { cost: number.of(10) },
  });

  hostApi.runtime.setEntity({
    id: string.of("item-3"),
    numberMap: { cost: number.of(15) },
  });

  hostApi.runtime.setEntity({
    id: string.of("bag-1"),
    entityListMap: { members: [] },
  });

  const addToBagEffect = hostApi.runtime.setEffect({
    name: string.of("add-to-bag"),
    apply: (context) => {
      const item = hostApi.runtime.setEntity({
        id: string.of("item-3"),
        numberMap: { cost: number.of(15) },
      });

      context.getMutableEntityBy({
        filters: [(c) => c.id.isContainingExactly(string.of("bag-1"))],
      }).forEach((container) => {
        container.addMember("members", item);
      });
    },
  });

  hostApi.runtime.setAction({
    name: string.of("add-to-bag"),
    apply: (context) => {
      context.emitEffect(addToBagEffect, {});
    },
  });
}
