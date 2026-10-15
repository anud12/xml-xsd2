/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  hostApi.runtime.setAnimation(hostApi.runtime.string.of("hover"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("hover.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.panel("hover", {
    panel: {
      width: 100,
      height: 100,
      onHover: {
        texture: hostApi.ui.getAnimation(hostApi.runtime.string.of("hover")),
        thickness: 5,
      },
      background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
    },
  })
  hostApi.ui.panel("no-hover", {
    panel: {
      width: 100,
      height: 100,
      background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
    },
  })
}
