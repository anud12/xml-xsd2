/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi;
  hostApi.registerAction({
    name:"action",
    apply: () => {
        hostApi.log("___From module action fired line___")
    }
  });

    hostApi.registerAction({
        name:"childAction",
        apply: () => {
            hostApi.log("___From module childAction fired line___")
        }
    });

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
      onClick: {
          type:"emitAction",
          actionName: string.of("action")
      },
    background: hostApi.texture.of("texture.exr"),
      children: [
        {
            id: "child",
            offset: {
                top: number.of(10),
                bottom: number.of(10),
                left: number.of(10),
                right: number.of(10),
            },
            size: {
                height: number.of(10),
                width: number.of(10),
            },
            background: hostApi.texture.of("texture_2.exr"),
            onClick: {
                type:"emitAction",
                actionName: string.of("childAction")
            }
        }
      ]
  })
}