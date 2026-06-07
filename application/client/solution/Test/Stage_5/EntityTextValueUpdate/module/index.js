/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi;

  hostApi.setEntity(string.of("entity_id"), {
    numberMap: {
      "key": number.of(0)
    }
  })
  
  hostApi.registerEffect({
    name:"repeat",
    reoccurAfterMs: (context, executionCount, input, output) => {
      return hostApi.maybe.of(number.of(10));
    },
    isReoccuranceApplicable: (context, executionCount, input, output) => {
      return hostApi.condition.of(true);
    },
    apply:(context, output) => {
      context.getEntityBy(hostApi.entity.filter.create().byId(id => id.isContainingExactly(string.of("entity_id"))))
        .map(elementExpr => {
          elementExpr.getNumber(string.of("key")).map(v => v.sum(number.of(1)));
        })
    }
  })
  
  hostApi.emitEvent("repeat", {});

  hostApi.registerPanel({
    id: "center",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    offset: {
      top: number.of(50),
      bottom: number.of(50),
      left: number.of(50),
      right: number.of(50),
    },
    content: {
      entityId: string.of("entity_id"),
      name: string.of("key"),
      type: "entityNumberValue",
      align: "center",
    },
    background: hostApi.texture.of("texture.exr")
  })
}