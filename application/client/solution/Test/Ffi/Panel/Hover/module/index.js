/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  hostApi.ui.registerPanel({
    id: "hover",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.ui.texture.of("texture.exr"),
    hover: {
      texture: hostApi.ui.texture.of("hover.exr"),
      thickness: 5,
    },
  })
  hostApi.ui.registerPanel({
    id: "no-hover",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.ui.texture.of("texture.exr"),
  })
}
