/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("hover.png"), {
    frames: [
      { sprite: hostApi.ui.texture.getSpritePNG("hover.png"), gtu: hostApi.runtime.number.of(10) },
    ],
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture.png"), {
    frames: [
      { sprite: hostApi.ui.texture.getSpritePNG("texture.png"), gtu: hostApi.runtime.number.of(10) },
    ],
  });
  hostApi.ui.registerPanel({
    id: "hoverPanel",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    offset: {
      top: number.of(100),
      bottom: number.of(100),
      left: number.of(100),
      right: number.of(100),
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png")),
    hover: {
      texture: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("hover.png")),
      thickness: 10,
    },
  })
}
