/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("border"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("border.png") },
    ],
  });
  hostApi.ui.panel("default", {
    width: 120,
    height: 120,
    border: { texture: hostApi.ui.getAnimation(hostApi.runtime.string.of("border"), { duration: hostApi.runtime.number.of(1) }) },
  });
}
