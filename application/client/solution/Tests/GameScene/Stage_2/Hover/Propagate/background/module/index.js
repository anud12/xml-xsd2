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
  hostApi.ui.window("hoverParent", {
    panel: {
      width: 100,
      height: 100,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
      onHover: {
        background: { animation: hover, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
      },
    },
  }, [
    hostApi.ui.window("inner", {
      panel: {
        width: 30,
        height: 30,
        x: 20,
        y: 20,
      },
    }, [])
  ])
}
