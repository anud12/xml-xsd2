/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const border = hostApi.runtime.setAnimation(hostApi.runtime.string.of("border"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("border.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.window("default", {
    panel: {
      width: 120,
      height: 120,
      border: { texture: { animation: border, fill: { width: "stretch", height: "stretch" } }},
    },
  });
}
