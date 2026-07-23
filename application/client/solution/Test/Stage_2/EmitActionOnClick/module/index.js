/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.registerAction({
    name:"action",
    apply: () => {
        hostApi.runtime.log("___From module action fired line___")
    }
  });

    hostApi.runtime.registerAction({
        name:"childAction",
        apply: () => {
            hostApi.runtime.log("___From module childAction fired line___")
        }
    });

  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture.png"), {
    frames: [
      { sprite: hostApi.ui.texture.getSpritePNG("texture.png"), gtu: hostApi.runtime.number.of(10) },
    ],
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture_2.png"), {
    frames: [
      { sprite: hostApi.ui.texture.getSpritePNG("texture_2.png"), gtu: hostApi.runtime.number.of(10) },
    ],
  });
  hostApi.ui.registerPanel({
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
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png")),
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
            background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture_2.png")),
            onClick: {
                type:"emitAction",
                actionName: string.of("childAction")
            }
        }
      ]
  })
}