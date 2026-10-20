/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const background = hostApi.runtime.setAnimation(hostApi.runtime.string.of("background"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("background.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  const border = hostApi.runtime.setAnimation(hostApi.runtime.string.of("border"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("border.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });

  // Every textured slot is the unified { animation, fill, nineSlice? } shape.
  // The explicit fills flow through the whole pipeline (module -> host.js ->
  // runtime ABI -> slab -> reader) and override the slot defaults:
  //   background  fill stretch        (default: cover / keepAspectRatio)
  //   border      fill stretch + 9s 3 (default: 9-slice frame, no tile)
  //   hover       fill stretch + 9s 2 (default: 9-slice frame at thickness)
  hostApi.ui.window("painted", {
    panel: {
      width: 120,
      height: 80,
      // Stretch (distorting) instead of the default cover.
      background: {
        animation: background,
        fill: { width: "stretch", height: "stretch" },
      },
      // Nine-slice with a 3px frame; the (transparent) source center stretches.
      border: {
        width: 3,
        texture: {
          animation: border,
          fill: { width: "stretch", height: "stretch" },
          nineSlice: { width: 3, height: 3 },
        },
      },
      // Hover outline nine-slices at 2px.
      onHover: {
        thickness: 2,
        texture: {
          animation: border,
          fill: { width: "stretch", height: "stretch" },
          nineSlice: { width: 2, height: 2 },
        },
      },
    },
  });
}
