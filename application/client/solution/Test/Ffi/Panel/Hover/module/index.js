/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number} = hostApi.runtime;
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("hover"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("hover.png") },
    ],
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.registerPanel({
    id: "hover",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(1) }),
    hover: {
      texture: hostApi.ui.getAnimation(hostApi.runtime.string.of("hover"), { duration: hostApi.runtime.number.of(1) }),
      thickness: 5,
    },
  })
  hostApi.ui.registerPanel({
    id: "no-hover",
    size: {
      height: number.of(100),
      width: number.of(100)
    },
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(1) }),
  })
}
