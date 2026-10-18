/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  const hover = hostApi.runtime.setAnimation(hostApi.runtime.string.of("hover"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("hover.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.panel("parent", {
    panel: {
      width: 100,
      height: 100,
      background: texture,
      onHover: {
        background: hover,
      },
    },
  }, [
    hostApi.ui.panel("child", {
      panel: {
        width: 20,
        height: 20,
        x: 40,
        y: 40,
        background: texture,
        onHover: {
          background: hover,
          stopPropagation: true,
        },
      },
    }, [])
  ])
}
