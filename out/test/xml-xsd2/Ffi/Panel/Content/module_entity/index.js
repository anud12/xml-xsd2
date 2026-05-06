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
      type: "entityTextValue",
      entityId: hostApi.string.of("entityId"),
      name: string.of("playerName"),
      align: "center"
    },
    background: hostApi.texture.of("texture.exr"),
  })
}
