/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF", style: [] });
 const {number, string} = hostApi.runtime;

  const entity = hostApi.runtime.setEntity({
    id: string.of("entity_id"),
    textMap: {
      "textKey": string.of("textValue")
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
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [
    hostApi.ui.field("content", { entity: entity, map: "text", name: "textKey", fallback: "" }, font),
  ])
}