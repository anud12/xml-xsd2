/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.registerAction({
    name: string.of("action"),
    apply: () => {
        hostApi.runtime.log("___From module action fired line___")
    }
  });
  hostApi.runtime.registerAction({
    name: string.of("childAction"),
    apply: () => {
        hostApi.runtime.log("___From module childAction fired line___")
    }
  });
  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.runtime.registerAnimation(string.of("texture_2"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture_2.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("center", {
    width: 100,
    height: 100,
    background: hostApi.ui.getAnimation(string.of("texture")),
    onClick: (ctx) => {
      ctx.emitAction("action");
    },
  }, [
    hostApi.ui.panel("child", {
      x: 20,
      y: 20,
      width: 10,
      height: 10,
      background: hostApi.ui.getAnimation(string.of("texture_2")),
      onClick: (ctx) => {
        ctx.emitAction("childAction");
      },
    }, [])
  ])
}