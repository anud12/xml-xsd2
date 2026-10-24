const entry = (hostApi) => {
    const { string, number } = hostApi.runtime;

    hostApi.runtime.setEntity({
        id: string.of("obj"),
        numberMap: { score: number.of(0) },
    });

    const incrementEffect = hostApi.runtime.setEffect({
        name: string.of("increment"),
        apply: (context) => {
            context.getMutableEntityBy({
                filters: [(e) => e.id.isContainingExactly(string.of("obj"))],
            }).map((entity) => {
                entity.number_map.put("score", (current) =>
                    current.map(v => v.sum(number.of(1)))
                );
            });
        },
    });

    const clickAction = hostApi.runtime.setAction({
        name: string.of("click-action"),
        apply: (ctx) => {
            ctx.emitEffect(incrementEffect, {});
        },
    });

    hostApi.ui.window("btn", {
        panel: {
            x: 50,
            y: 50,
            width: 100,
            height: 40,
            onClick: (ctx) => { ctx.emitAction(clickAction); },
        },
    });
};

export default entry;
