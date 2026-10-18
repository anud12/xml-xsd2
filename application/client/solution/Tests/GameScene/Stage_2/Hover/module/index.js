/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  const hover = hostApi.runtime.setAnimation(string.of("hover"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("hover.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("hoverPanel", {
    panel: {
      width: 100,
      height: 100,
      background: texture,
      onHover: {
        texture: hover,
        thickness: 10,
      },
    },
  }, [])
}
