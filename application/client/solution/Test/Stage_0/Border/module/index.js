/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("background"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("background.png") },
    ],
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("border"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("border.png") },
    ],
  });
  hostApi.ui.panel("bordered", {
    width: 120,
    height: 120,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("background"), { duration: hostApi.runtime.number.of(1) }),
    border: { width: 3, texture: hostApi.ui.getAnimation(hostApi.runtime.string.of("border"), { duration: hostApi.runtime.number.of(1) }) },
  });
}
