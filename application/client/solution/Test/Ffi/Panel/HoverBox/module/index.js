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
    hoverBox: {
      texture: hostApi.texture.of("hover.exr"),
      thickness: 5
    }
  })
}
