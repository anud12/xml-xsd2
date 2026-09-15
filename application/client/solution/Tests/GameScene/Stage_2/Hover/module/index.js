/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.runtime.registerAnimation(string.of("hover"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("hover.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("hoverPanel", {
    width: 100,
    height: 100,
    background: hostApi.ui.getAnimation(string.of("texture")),
    onHover: {
      texture: hostApi.ui.getAnimation(string.of("hover")),
      thickness: 10,
    },
  }, [])
}