/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const border = hostApi.runtime.setAnimation(hostApi.runtime.string.of("border"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("border.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.panel("default", {
    panel: {
      width: 120,
      height: 120,
      border: { texture: border },
    },
  });
}
