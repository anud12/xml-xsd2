/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });

  // Screen A's clickable child: swaps the current screen to screen B.
  const target = hostApi.ui.window("s1-target", {
    panel: {
      x: 50,
      y: 50,
      width: 100,
      height: 40,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
      onClick: () => {
        hostApi.ui.setScreen(
          { screen: { background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } }} },
          [hostApi.ui.text("s2-label", "screen-b")]
        );
      },
    },
  });

  // The current screen: full viewport, below app buttons and windows.
  hostApi.ui.setScreen(
    { screen: { background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } }} },
    [target]
  );

  // A regular window: above the screen.
  hostApi.ui.window("top-win", {
    panel: {
      x: 400,
      y: 400,
      width: 50,
      height: 50,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  });
};
