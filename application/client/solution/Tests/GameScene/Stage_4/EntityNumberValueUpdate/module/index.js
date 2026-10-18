/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const entity = hostApi.runtime.setEntity({
    id: string.of("entity_id"),
    numberMap: {
      "numberKey": number.of(42)
    }
  })

  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.window("center", {
    panel: {
      width: 100,
      height: 100,
      anchor: "center",
      align: "center",
      background: texture,
    },
  }, [
    hostApi.ui.field("content", { entity: entity, map: "number", name: "numberKey", fallback: "0" }),
  ])
}
