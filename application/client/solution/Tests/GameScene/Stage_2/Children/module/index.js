/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  const texture_2 = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture_2"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture_2.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.window("center", {
    panel: {
      width: 100,
      height: 100,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [
    hostApi.ui.window("child", {
      panel: {
        width: 10,
        height: 10,
        x: 0,
        y: 0,
        background: { animation: texture_2, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
      },
    }, []),
    hostApi.ui.window("child_2", {
      panel: {
        width: 10,
        height: 10,
        x: 0,
        y: 10,
        background: { animation: texture_2, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
      },
    }, []),
  ])
}
