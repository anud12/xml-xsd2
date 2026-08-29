/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.setEntity(string.of("entity_id"), {
    textMap: {
      "textKey": string.of("textValue")
    }
  });
  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.window("top-right", {
    width: 300,
    height: 300,
    background: hostApi.ui.getAnimation(string.of("texture"), { duration: number.of(1) }),
    align: "center",
  }, [
    hostApi.ui.field("content", { entity: "entity_id", map: "text", name: "textKey", fallback: "" }),
  ])
}
