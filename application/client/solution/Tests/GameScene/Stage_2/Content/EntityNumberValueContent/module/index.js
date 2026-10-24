/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF", style: [] });
 const {number, string} = hostApi.runtime;
  const entity = hostApi.runtime.setEntity({
    id: string.of("entity_id"),
    numberMap: {
      "numberKey": number.of(42)
    }
  });
  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.window("number-panel", {
    panel: {
      width: 300,
      height: 300,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
      align: "center",
    },
  }, [
    hostApi.ui.field("content", { entity: entity, map: "number", name: "numberKey", fallback: "0" }, font),
  ])
}