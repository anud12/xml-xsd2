/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  
  hostApi.runtime.setEntity(string.of("entity_id"), {
    numberMap: {
      "numberKey": number.of(42)
    }
  })

  hostApi.ui.registerPanel({
    id: "number-panel",
    size: {
      height: number.of(300),
      width: number.of(300)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    background: hostApi.ui.texture.of("texture.png"),
    content: {
      align: "center",
      entityId: string.of("entity_id"),
      name: string.of("numberKey"),
      type: "entityNumberValue",
    }
  })
}
