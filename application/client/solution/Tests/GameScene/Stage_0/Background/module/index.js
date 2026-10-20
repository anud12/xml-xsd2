/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  // Centered: top-left at the viewport center (500, 500).
  hostApi.ui.window("panel", {
    panel: {
      width: 100,
      height: 100,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [])
  // Covers the top-left quarter of panel — panel must still pass.
  hostApi.ui.window("overlay", {
    panel: {
      width: 50,
      height: 50,
      x: 500,
      y: 500,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [])
  // No background: used by the negative "window without background" case.
  hostApi.ui.window("plain", {
    panel: {
      width: 50,
      height: 50,
      x: 20,
      y: 20,
    },
  }, [])
}
