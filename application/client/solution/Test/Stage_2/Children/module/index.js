/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number } = hostApi.runtime;
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture_2"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture_2.png") },
    ],
  });
  hostApi.ui.registerPanel({
    id: "center",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), hostApi.runtime.number.of(1)),
    children: [
      {
        id: "child",
        size: {
          height: number.of(10),
          width: number.of(10),
        },
        background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture_2"), hostApi.runtime.number.of(1))
      },
      {
        id: "child_2",
        size: {
          height: number.of(10),
          width: number.of(10),
        },
        background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture_2"), hostApi.runtime.number.of(1))
      },
    ]
  })
}