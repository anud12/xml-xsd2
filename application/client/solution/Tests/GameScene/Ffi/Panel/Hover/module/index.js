/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  const hover = hostApi.runtime.setAnimation(hostApi.runtime.string.of("hover"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("hover.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.window("hover", {
    panel: {
      width: 100,
      height: 100,
      onHover: {
        texture: hover,
        thickness: 5,
      },
      background: texture,
    },
  })
  hostApi.ui.window("no-hover", {
    panel: {
      width: 100,
      height: 100,
      background: texture,
    },
  })
}
