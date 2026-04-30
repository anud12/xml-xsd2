/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi;

  hostApi.setEntity(string.of("entity_id"), {
    textMap: {
      "textKey": string.of("textValue")
    }
  })

  hostApi.registerPanel({
    id: "center",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    offset: {
      top: number.of(50),
      bottom: number.of(50),
      left: number.of(50),
      right: number.of(50),
    },
    content: {
      entityId: string.of("entity_id"),
      name: string.of("textKey"),
      type: "entityTextValue",
      align: "center",
    },
    background: hostApi.texture.of("texture.exr")
  })
}