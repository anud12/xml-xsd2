/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { string, number } = hostApi.runtime;

  const red = hostApi.runtime.setAnimation(string.of("red"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("red.png") }],
    duration: number.of(1),
  });
  const green = hostApi.runtime.setAnimation(string.of("green"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("green.png") }],
    duration: number.of(1),
  });
  const blue = hostApi.runtime.setAnimation(string.of("blue"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("blue.png") }],
    duration: number.of(1),
  });

  hostApi.ui.window("nested", {
    panel: {
      width: 200,
      height: 60,
      layout: "row",
    },
  }, [
    hostApi.ui.panel("x", {
      panel: {
        width: 100,
        height: 60,
        background: {
          animation: red,
          fill: { width: "stretch", height: "stretch" },
        },
      },
    }),
    hostApi.ui.panel("stats", {
      panel: {
        width: 100,
        height: 60,
        layout: "column",
      },
    }, [
      hostApi.ui.panel("y", {
        panel: {
          width: 100,
          height: 30,
          background: {
            animation: green,
            fill: { width: "stretch", height: "stretch" },
          },
        },
      }),
      hostApi.ui.panel("z", {
        panel: {
          width: 100,
          height: 30,
          background: {
            animation: blue,
            fill: { width: "stretch", height: "stretch" },
          },
        },
      }),
    ]),
  ]);
}
