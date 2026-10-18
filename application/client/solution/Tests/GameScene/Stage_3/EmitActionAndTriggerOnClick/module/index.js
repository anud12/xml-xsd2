/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const {number, string} = hostApi.runtime;
    const effect = hostApi.runtime.setEffect({
        name: string.of("effect"),

        prepare: () => {
            hostApi.runtime.log("___From module effect prepare fired line___")
            return {}
        },
        apply: () => {
            hostApi.runtime.log("___From module effect fired line___")
        }
    })

    const action = hostApi.runtime.setAction({
        name: string.of("action"),
        apply: (context) => {
          context.actor.containers
            hostApi.runtime.log("___From module action fired line___")
            context.emitEffect(effect, {})
        }
    });

    const texture = hostApi.runtime.setAnimation(string.of("texture"), {
        frames: [
          { sprite: hostApi.ui.getSpritePNG("texture.png") },
        ],
        duration: number.of(1),
      });

    hostApi.ui.window("center", {
        panel: {
            width: 100,
            height: 100,
            anchor: "center",
            background: texture,
            onClick: (ctx) => {
                ctx.emitAction(action);
            },
        },
    }, [])
}
