/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi;
  hostApi.registerPanel({
    id: "panel_entity",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    content: {
      type: "entityStringValue",
      name: string.of("playerName")
    },
    background: hostApi.texture.of("texture.exr"),
  })
}
