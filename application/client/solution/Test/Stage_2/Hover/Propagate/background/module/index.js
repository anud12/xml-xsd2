/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("hover"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("hover.png") },
    ],
  });
  hostApi.ui.panel("hoverParent", {
    width: 100,
    height: 100,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(1) }),
    onHover: {
      background: hostApi.ui.getAnimation(hostApi.runtime.string.of("hover"), { duration: hostApi.runtime.number.of(1) }),
    },
  }, [
    hostApi.ui.panel("inner", {
      width: 30,
      height: 30,
      x: 20,
      y: 20,
    }, [])
  ])
}
