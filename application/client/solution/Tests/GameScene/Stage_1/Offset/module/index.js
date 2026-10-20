/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.window("top", {
    panel: {
      width: 10,
      height: 10,
      x: 495,
      y: 395,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [])
  hostApi.ui.window("left", {
    panel: {
      width: 10,
      height: 10,
      x: 395,
      y: 495,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [])
  hostApi.ui.window("bottom", {
    panel: {
      width: 10,
      height: 10,
      x: 495,
      y: 595,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [])
  hostApi.ui.window("right", {
    panel: {
      width: 10,
      height: 10,
      x: 595,
      y: 495,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [])
}
