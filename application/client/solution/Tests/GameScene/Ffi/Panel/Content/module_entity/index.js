/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF", style: [] });
 const {number, string} = hostApi.runtime;
  const entityId = hostApi.runtime.setEntity({
    id: string.of("entityId"),
  });
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.window("panel_entity", {
    panel: {
      width: 100,
      height: 100,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [
    hostApi.ui.field("panel_entity-content", { entity: entityId, map: "text", name: "playerName", fallback: "" }, font),
  ])
}