/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  hostApi.runtime.setEntity(string.of("entity_id"), {
    numberMap: {
      "numberKey": number.of(42)
    }
  })

  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("center", {
    width: 100,
    height: 100,
    anchor: "center",
    align: "center",
    background: hostApi.ui.getAnimation(string.of("texture")),
  }, [
    hostApi.ui.field("content", { entity: "entity_id", map: "number", name: "numberKey", fallback: "0" }),
  ])
}