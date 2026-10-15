/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  const entityId = hostApi.runtime.setEntity({
    id: string.of("entityId"),
  });
  hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.panel("panel_entity", {
    panel: {
      width: 100,
      height: 100,
      background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
    },
  }, [
    hostApi.ui.field("panel_entity-content", { entity: entityId, map: "text", name: "playerName", fallback: "" }),
  ])
}
