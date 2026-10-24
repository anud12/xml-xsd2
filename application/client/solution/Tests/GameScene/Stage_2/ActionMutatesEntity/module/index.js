const entry = (hostApi) => {
    const { string, number, maybe } = hostApi.runtime;

    const obj = hostApi.runtime.setEntity({
        id: string.of("obj"),
        numberMap: {
            counter: number.of(100),
        },
    });

    const tick = hostApi.runtime.setEffect({
        name: string.of("tick"),
        reoccurAfterMs: (context) => {
            const current = Number(context.getEntityBy({
                filters: [(e) => e.id.isContainingExactly(string.of("obj"))],
            }).map((el) => el.number_map.get("counter").orElse(number.of(0))));
            if (current <= 0) return maybe.none();
            return maybe.of(number.of(1));
        },
        apply: (context) => {
            context.getMutableEntityBy({
                filters: [(e) => e.id.isContainingExactly(string.of("obj"))],
            }).map((entity) => {
                entity.number_map.put("counter", (current) =>
                    current.map(v => v.sum(number.of(-1)))
                );
            });
        },
    });

    hostApi.runtime.emitEvent(tick, {});

    const resetEffect = hostApi.runtime.setEffect({
        name: string.of("reset-event"),
        apply: (context) => {
            context.getMutableEntityBy({
                filters: [(e) => e.id.isContainingExactly(string.of("obj"))],
            }).map((entity) => {
                entity.number_map.put("counter", () =>
                    hostApi.runtime.maybe.of(number.of(100))
                );
            });
        },
    });

    const reset = hostApi.runtime.setAction({
        name: string.of("reset"),
        apply: (ctx) => {
            ctx.emitEvent("reset-event");
        },
    });

    hostApi.ui.window("btn", {
        panel: {
            x: 100,
            y: 100,
            width: 80,
            height: 30,
            onClick: (ctx) => {
                ctx.emitAction(reset);
            },
        },
    });
};

export default entry;
