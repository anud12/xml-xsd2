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
      background: texture,
    },
  }, [])
  hostApi.ui.window("left", {
    panel: {
      width: 10,
      height: 10,
      x: 395,
      y: 495,
      background: texture,
    },
  }, [])
  hostApi.ui.window("bottom", {
    panel: {
      width: 10,
      height: 10,
      x: 495,
      y: 595,
      background: texture,
    },
  }, [])
  hostApi.ui.window("right", {
    panel: {
      width: 10,
      height: 10,
      x: 595,
      y: 495,
      background: texture,
    },
  }, [])
}
