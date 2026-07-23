/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  hostApi.runtime.setEntity(string.of("entity_id"), {
    numberMap: {
      "numberKey": number.of(42)
    }
  })

  hostApi.ui.registerPanel({
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
      name: string.of("numberKey"),
      type: "entityNumberValue",
      align: "center",
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png"))
  })
}
