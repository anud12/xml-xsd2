/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  const action = hostApi.runtime.setAction({
    name: string.of("action"),
    apply: () => {
        hostApi.runtime.log("___From module action fired line___")
    }
  });
  const childAction = hostApi.runtime.setAction({
    name: string.of("childAction"),
    apply: () => {
        hostApi.runtime.log("___From module childAction fired line___")
    }
  });
  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  const texture_2 = hostApi.runtime.setAnimation(string.of("texture_2"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture_2.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.window("center", {
    panel: {
      width: 100,
      height: 100,
      background: texture,
      onClick: (ctx) => {
        ctx.emitAction(action);
      },
    },
  }, [
    hostApi.ui.window("child", {
      panel: {
        x: 20,
        y: 20,
        width: 10,
        height: 10,
        background: texture_2,
        onClick: (ctx) => {
          ctx.emitAction(childAction);
        },
      },
    }, [])
  ])
}
