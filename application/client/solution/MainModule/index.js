/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi;

  hostApi.setEntity(string.of("ticker"), {
    numberMap: {
      "count": number.of(0)
    }
  });

  hostApi.registerAction({
    name: "start",
    apply: (ctx) => {
      hostApi.log("___From module action fired line___")
      ctx.emitEffect("tick", {});
    }
  });

  hostApi.registerEffect({
    name: "tick",
    apply: (context, output) => {
      context.getEntityBy(hostApi.entity.filter.create().byId(id => id.isContainingExactly(string.of("ticker"))))
        .map(elementExpr => {
          elementExpr.getNumber(hostApi.string.of("count")).map(v => v.sum(number.of(1)))
        })
    }
  });

  hostApi.registerPanel({
    id: "counter",
    size: {
      height: number.of(200),
      width: number.of(200)
    },
    offset: {
      top: number.of(100),
      bottom: number.of(100),
      left: number.of(100),
      right: number.of(100),
    },
    onClick: {
      type:"emitAction",
      actionName: string.of("start")
    },
    background: hostApi.texture.of("texture.exr")
  });


  hostApi.registerPanel({
    id: "viewer",
    size: {
      height: number.of(200),
      width: number.of(200)
    },
    offset: {
      top: number.of(300),
      bottom: number.of(300),
      left: number.of(300),
      right: number.of(300),
    },
    content: {
      entityId: string.of("ticker"),
      name: string.of("count"),
      type: "entityNumberValue",
      align: "center",
    },
    background: hostApi.texture.of("texture.exr")
  });
}
