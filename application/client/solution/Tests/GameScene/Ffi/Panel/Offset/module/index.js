/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.window("top", {
    panel: {
      x: 0,
      y: -100,
      width: 10,
      height: 10,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  })
  hostApi.ui.window("left", {
    panel: {
      x: -100,
      y: 0,
      width: 10,
      height: 10,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  })
  hostApi.ui.window("bottom", {
    panel: {
      x: 0,
      y: 100,
      width: 10,
      height: 10,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  })
  hostApi.ui.window("right", {
    panel: {
      x: 100,
      y: 0,
      width: 10,
      height: 10,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  })
}
