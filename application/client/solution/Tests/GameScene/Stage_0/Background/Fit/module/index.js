/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const background = hostApi.runtime.setAnimation(hostApi.runtime.string.of("background"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("background.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });

  // Fit fill: uniform scale (min), centered, letterboxed (contain).
  hostApi.ui.window("fitted", {
    panel: {
      width: 100,
      height: 100,
      background: {
        animation: background,
        fill: { width: "fit", height: "fit" },
      },
    },
  });
}
