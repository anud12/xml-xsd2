/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const background = hostApi.runtime.setAnimation(hostApi.runtime.string.of("background"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("background.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  const border = hostApi.runtime.setAnimation(hostApi.runtime.string.of("border"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("border.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.window("bordered", {
    panel: {
      width: 120,
      height: 120,
      background: { animation: background, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
      border: { width: 3, texture: { animation: border, fill: { width: "stretch", height: "stretch" } }},
    },
  });
}
