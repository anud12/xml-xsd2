/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi;
  const filter = hostApi.entity.filter.create().byId(id => id.isContainingExactly(string.of("entity_id")));
  const key = string.of("key");
  
  hostApi.setEntity(string.of("entity_id"), {
    numberMap: {
      "key": number.of(0)
    }
  })

  hostApi.registerEffect({
    name: "repeat",
    reoccurAfterMs: (context, executionCount, input, output) => {
      return context.getEntityBy(filter)
        .get(number.of(0))
        .flatMap(elementExpr => elementExpr.getNumber(key))
        .isCondition(value => value.isLessOrEqualTo(number.of(20)))
        .getOnTrueOrFalse(hostApi.maybe.of(hostApi.number.of(1)), hostApi.maybe.none());
    },
    apply: (context, output) => {
      context.getEntityBy(filter)
        .map(elementExpr => {
          elementExpr.getNumber(key).map(v => v.sum(number.of(1)));
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
      align: "top",
    },
    background: hostApi.texture.of("texture.exr")
  })
}
