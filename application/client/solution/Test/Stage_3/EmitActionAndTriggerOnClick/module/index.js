/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const {number, string} = hostApi.runtime;
    hostApi.runtime.registerAction({
        name: string.of("action"),
        apply: (context) => {
            hostApi.runtime.log("___From module action fired line___")
            context.emitEffect("effect", {})
        }
    });

    hostApi.runtime.registerEffect({
        name: "effect",

        prepare: () => {
            hostApi.runtime.log("___From module effect prepare fired line___")
            return {}
        },
        apply: () => {
            hostApi.runtime.log("___From module effect fired line___")
        }
    })

    hostApi.runtime.registerAnimation(string.of("texture"), {
        frames: [
          { sprite: hostApi.ui.getSpritePNG("texture.png") },
        ],
      });

    hostApi.ui.window("center", {
        width: 100,
        height: 100,
        anchor: "center",
        background: hostApi.ui.getAnimation(string.of("texture"), { duration: number.of(1) }),
        onClick: "action",
    }, [])
}
