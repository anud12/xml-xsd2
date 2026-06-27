/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi;
  hostApi.registerPanel({
    id: "hover",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.texture.of("texture.exr"),
    hover: {
      texture: hostApi.texture.of("hover.exr"),
      thickness: 5,
    },
  })
  hostApi.registerPanel({
    id: "no-hover",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.texture.of("texture.exr"),
  })
}
