/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi;
  hostApi.registerPanel({
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
    background: hostApi.texture.of("texture.exr"),
    hover: {
      texture: hostApi.texture.of("hover.exr"),
      thickness: 10,
    },
  })
}
