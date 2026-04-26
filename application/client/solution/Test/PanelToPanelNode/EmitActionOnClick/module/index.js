/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi;
  hostApi.registerAction({
    name:"action",
    apply: () => {
        hostApi.log("action fired")
    }
  });

  hostApi.registerPanel({
    id: "center",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.texture.of("texture.exr"),
    children: [{
        id:"child",
        size: {
          height: number.of(10),
          width: number.of(10),
        },
        onClick: {
          type:"emitAction",
          actionName: string.of("action")
        },
        background: hostApi.texture.of("texture_2.exr"),
    }]
  })
}