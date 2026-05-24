/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi;
  
  hostApi.setEntity(string.of("entity_id"), {
    textMap: {
      "textKey": string.of("textValue")
    }
  })

  hostApi.registerPanel({
    id: "top-right",
    size: {
      height: number.of(300),
      width: number.of(300)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    background: hostApi.texture.of("texture.exr"),
    content: {
      align: "center",
      entityId: string.of("entity_id"),
      value: (entity) => entity.getText(string.of("textKey")).orElse(string.of("None")),
      type: "entityTextValue",
    }
  })
}