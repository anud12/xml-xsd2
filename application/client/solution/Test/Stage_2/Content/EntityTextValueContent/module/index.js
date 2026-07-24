/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  
  hostApi.runtime.setEntity(string.of("entity_id"), {
    textMap: {
      "textKey": string.of("textValue")
    }
  })

  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png"), gtu: hostApi.runtime.number.of(10) },
    ],
  });
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
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
    content: {
      align: "center",
      entityId: string.of("entity_id"),
      name: string.of("textKey"),
      type: "entityTextValue",
    }
  })
}