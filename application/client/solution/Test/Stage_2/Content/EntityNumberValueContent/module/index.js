/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.setEntity(string.of("entity_id"), {
    numberMap: {
      "numberKey": number.of(42)
    }
  });
  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.panel("number-panel", {
    width: 300,
    height: 300,
    background: hostApi.ui.getAnimation(string.of("texture"), { duration: number.of(1) }),
    align: "center",
  }, [
    hostApi.ui.field("content", { entity: "entity_id", map: "number", name: "numberKey", fallback: "0" }),
  ])
}
