/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  hostApi.ui.registerPanel({
    id: "hover",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png")),
    hover: {
      texture: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("hover.png")),
      thickness: 5,
    },
  })
  hostApi.ui.registerPanel({
    id: "no-hover",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png")),
  })
}
