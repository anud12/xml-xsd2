/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const entity = hostApi.runtime.setEntity({
    id: string.of("entity_id"),
    textMap: {
      "textKey": string.of("textValue")
    }
  })

  hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("center", {
    panel: {
      width: 100,
      height: 100,
      anchor: "center",
      align: "center",
      background: hostApi.ui.getAnimation(string.of("texture")),
    },
  }, [
    hostApi.ui.field("content", { entity: entity, map: "text", name: "textKey", fallback: "" }),
  ])
}
