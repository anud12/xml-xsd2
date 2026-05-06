/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi;
  hostApi.registerPanel({
    id: "center",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    background: hostApi.texture.of("texture.exr"),
  })
}