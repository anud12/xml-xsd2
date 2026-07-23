/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
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
