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
  // Overlays the top-left quarter of panel — the region under the overlay
  // must show the overlay's background, not the panel's.
  hostApi.ui.window("overlay", {
    panel: {
      width: 50,
      height: 50,
      x: 500,
      y: 500,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [])
}
