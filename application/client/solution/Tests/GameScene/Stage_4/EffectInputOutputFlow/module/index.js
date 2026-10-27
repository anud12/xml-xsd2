/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });
    const {number, string} = hostApi.runtime;

    const entity = hostApi.runtime.setEntity({
        id: string.of("game"),
        numberMap: {
            "result": number.of(0)
        }
    });

    const compute = hostApi.runtime.setEffect({
        name: string.of("compute"),
        // `input` is the payload delivered by emitEvent; the value returned
        // here is delivered to `apply` as `output`.
        prepare: (context, input) => {
            return input.base + 10;
        },
        apply: (context, output) => {
            context.getMutableEntityBy({
                filters: [(e) => e.id.isContainingExactly(string.of("game"))],
            }).map((game) => {
                game.number_map.put("result", () => hostApi.runtime.maybe.of(output));
            });
        },
    });

    // base=42 travels to prepare's `input`; prepare returns 52; apply writes 52.
    hostApi.runtime.emitEvent(compute, { base: number.of(42) });

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
            align: "center",
            background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
        },
    }, [
        hostApi.ui.field("content", { entity: entity, map: "number", name: "result", fallback: "0" }, font),
    ]);
}
