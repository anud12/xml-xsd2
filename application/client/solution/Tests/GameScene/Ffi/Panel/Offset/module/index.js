/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.panel("top", {
    panel: {
      x: 0,
      y: -100,
      width: 10,
      height: 10,
      background: texture,
    },
  })
  hostApi.ui.panel("left", {
    panel: {
      x: -100,
      y: 0,
      width: 10,
      height: 10,
      background: texture,
    },
  })
  hostApi.ui.panel("bottom", {
    panel: {
      x: 0,
      y: 100,
      width: 10,
      height: 10,
      background: texture,
    },
  })
  hostApi.ui.panel("right", {
    panel: {
      x: 100,
      y: 0,
      width: 10,
      height: 10,
      background: texture,
    },
  })
}
