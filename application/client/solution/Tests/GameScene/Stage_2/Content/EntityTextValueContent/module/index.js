/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  const entity = hostApi.runtime.setEntity({
    id: string.of("entity_id"),
    textMap: {
      "textKey": string.of("textValue")
    }
  });
  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("top-right", {
    panel: {
      width: 300,
      height: 300,
      background: texture,
      align: "center",
    },
  }, [
    hostApi.ui.field("content", { entity: entity, map: "text", name: "textKey", fallback: "" }),
  ])
}
