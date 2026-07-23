/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  
  hostApi.runtime.setEntity(string.of("entity_id"), {
    textMap: {
      "textKey": string.of("textValue")
    }
  })

  hostApi.ui.registerPanel({
    id: "top-right",
    size: {
      height: number.of(300),
      width: number.of(300)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png")),
    content: {
      align: "center",
      entityId: string.of("entity_id"),
      name: string.of("textKey"),
      type: "entityTextValue",
    }
  })
}