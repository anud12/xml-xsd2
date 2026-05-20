/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi;

  hostApi.setEntity(string.of("entity_id"), {
    numberMap: {
      "textKey": number.of(1)
    }
  })

  hostApi.registerEffect({
    name: "increment_text_key",
    reoccurAfterMs: (context, executionCount, input, output) => {
      return hostApi.maybe.of(number.of(100))
    },
    apply: context => {
      context.getEntityBy(hostApi.entity.filter.create()
        .byId(id => id.isContainingExactly(string.of("entity_id"))))
        .map(elementExpr => elementExpr.getNumber(string.of("textKey")).map(v => v.sum(number.of(1))));
    }
  })

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
      name: string.of("textKey"),
      type: "entityNumberValue",
      align: "center",
    },
    background: hostApi.texture.of("texture.exr")
  })

}