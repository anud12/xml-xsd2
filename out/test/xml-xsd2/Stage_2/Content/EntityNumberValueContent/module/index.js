/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi;
  
  hostApi.setEntity(string.of("entity_id"), {
    numberMap: {
      "numberKey": number.of(42)
    }
  })

  hostApi.registerPanel({
    id: "number-panel",
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
      name: string.of("numberKey"),
      type: "entityNumberValue",
    }
  })
}
