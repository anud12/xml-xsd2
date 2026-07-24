/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  
  hostApi.runtime.setEntity(string.of("entity_id"), {
    numberMap: {
      "numberKey": number.of(42)
    }
  })

  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
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
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), hostApi.runtime.number.of(1)),
    content: {
      align: "center",
      entityId: string.of("entity_id"),
      name: string.of("numberKey"),
      type: "entityNumberValue",
    }
  })
}
