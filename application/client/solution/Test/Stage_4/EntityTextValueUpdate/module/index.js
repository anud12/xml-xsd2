/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  hostApi.runtime.setEntity(string.of("entity_id"), {
    textMap: {
      "textKey": string.of("textValue")
    }
  })

  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.panel("center", {
    width: 100,
    height: 100,
    anchor: "center",
    align: "center",
    background: hostApi.ui.getAnimation(string.of("texture"), { duration: number.of(1) }),
  }, [
    hostApi.ui.field("content", { entity: "entity_id", map: "text", name: "textKey", fallback: "" }),
  ])
}
