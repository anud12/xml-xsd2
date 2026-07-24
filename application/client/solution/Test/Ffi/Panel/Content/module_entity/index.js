/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.registerPanel({
    id: "panel_entity",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    content: {
      type: "entityTextValue",
      entityId: hostApi.runtime.string.of("entityId"),
      name: string.of("playerName"),
      align: "center"
    },
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
  })
}
