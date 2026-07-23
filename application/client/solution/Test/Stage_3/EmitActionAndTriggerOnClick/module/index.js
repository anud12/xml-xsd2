/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const {number, string} = hostApi.runtime;
    hostApi.runtime.registerAction({
        name: "action",
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

    hostApi.ui.registerPanel({
        id: "center",
        size: {
            height: number.of(100),
            width: number.of(100)
        },
        offset: {
            top: number.of(50),
            bottom: number.of(50),
            left: number.of(50),
            right: number.of(50),
        },
        onClick: {
            type: "emitAction",
            actionName: string.of("action")
        },
        background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png"))
    })
}