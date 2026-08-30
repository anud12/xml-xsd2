/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.runtime.registerAnimation(string.of("hover"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("hover.png") },
    ],
  });
  hostApi.ui.panel("hoverPanel", {
    width: 100,
    height: 100,
    background: hostApi.ui.getAnimation(string.of("texture"), { duration: number.of(1) }),
    onHover: {
      texture: hostApi.ui.getAnimation(string.of("hover"), { duration: number.of(1) }),
      thickness: 10,
    },
  }, [])
}
