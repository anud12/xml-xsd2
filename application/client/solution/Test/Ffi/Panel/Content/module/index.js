/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi;
  hostApi.registerPanel({
    id: "panel",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    content: {
      type: "constant",
      value: string.of("Content")
    },
    background: hostApi.texture.of("texture.exr"),
  })
}
