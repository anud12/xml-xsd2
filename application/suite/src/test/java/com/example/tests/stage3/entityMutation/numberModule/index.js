/** @type {ModuleEntrypoint} */
export default ({string, number, ...hostApi}) => {

  hostApi.setEntity(string.of("entityId"), {
    numberMap: {
      value: number.of(1),
    },
    textMap: {}
  })

  hostApi.registerEffect({
    name: "effect",
    apply: (context) => {
      const entityList = context.getEntityBy(hostApi.entity.filter.create()
        .byId(id => id.isContainingExactly(string.of("entityId"))))

      entityList.map(elementExpr => {
        elementExpr.getNumber(string.of("value"))
          .map(v => v.sum(number.of(1)))
      })
    }
  })

  hostApi.registerAction({
    name: "action",
    apply: context => {
      context.emitEffect("effect", {});
    }
  })
}