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
    duration: number.of(1),
  });
  hostApi.ui.panel("top-right", {
    width: 300,
    height: 300,
    background: hostApi.ui.getAnimation(string.of("texture")),
    align: "center",
  }, [
    hostApi.ui.field("content", { entity: "entity_id", map: "text", name: "textKey", fallback: "" }),
  ])
}