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
  hostApi.ui.panel("parent", {
    width: 100,
    height: 100,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
    onHover: {
      background: hostApi.ui.getAnimation(hostApi.runtime.string.of("hover")),
    },
  }, [
    hostApi.ui.panel("child", {
      width: 20,
      height: 20,
      x: 40,
      y: 40,
      background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
      onHover: {
        background: hostApi.ui.getAnimation(hostApi.runtime.string.of("hover")),
        stopPropagation: true,
      },
    }, [])
  ])
}