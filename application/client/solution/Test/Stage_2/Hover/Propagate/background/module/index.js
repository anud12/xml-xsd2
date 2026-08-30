/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("hover"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("hover.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.panel("hoverParent", {
    width: 100,
    height: 100,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
    onHover: {
      background: hostApi.ui.getAnimation(hostApi.runtime.string.of("hover")),
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