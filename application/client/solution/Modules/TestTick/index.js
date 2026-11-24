const MAP_CELLS = 10;
const MAP_WIDTH = 800;
const MAP_HEIGHT = 600;
const entry = (hostApi) => {
    const { number, string } = hostApi.runtime;
    const mcv = hostApi.runtime.setEntity({
        id: string.of("mcv"),
        numberMap: { movementSpeed: 10, resource: 70, x: 0, y: 0 },
        stringMap: { xKey: "x", yKey: "y" },
    });
    const map = hostApi.runtime.setEntity({
        id: string.of("mcv-map"),
        entityListMap: { members: [mcv] },
        numberMap: { sizeX: MAP_CELLS, sizeY: MAP_CELLS },
        stringMap: { outOfBoundsX: "clamp", outOfBoundsY: "clamp" },
    });
    const moveMcv = hostApi.runtime.setAction({
        name: string.of("move-mcv-to-cell"),
        apply: (ctx) => ctx.teleportTo({
            groupId: map, entityId: mcv, x: ctx.args.x, y: ctx.args.y, clamp: true,
        }),
    });
    const global = hostApi.runtime.setEntity({
        id: string.of("global"),
        numberMap: { Interest: 0 },
    });
    const portrait = hostApi.runtime.setAnimation(string.of("mcv-portrait"), {
        frames: [{ sprite: hostApi.ui.getSpritePNG("assets/mcv-portrait.png") }],
        duration: number.of(1),
        loop: true,
    });
    const mapCell = hostApi.runtime.setAnimation(string.of("mcv-map-cell"), {
        frames: [{ sprite: hostApi.ui.getSpritePNG("assets/map-cell.png") }],
        duration: number.of(1),
        loop: true,
    });
    hostApi.ui.setScreen({ screen: { anchorChild: "center" } }, [
        hostApi.ui.groupView("mcv-map-view", {
            group: map,
            list: "members",
            emptyCell: { animation: mapCell, fill: { width: "stretch", height: "stretch", keepAspectRatio: false } },
            panel: {
                width: MAP_WIDTH, height: MAP_HEIGHT,
                layout: { group: map },
                onClick: (ctx) => {
                    if (ctx.cursor) {
                        ctx.emitAction(moveMcv, { x: ctx.cursor.getX(), y: ctx.cursor.getY() });
                    }
                },
            },
        }, (item) => hostApi.ui.window(item.id + "-map-cell", { panel: {} }, [
            hostApi.ui.panel(item.id + "-map-marker", {
                panel: {
                    width: 32, height: 32,
                    background: { animation: portrait, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
                },
            }),
        ])),
    ]);
    hostApi.ui.window("mcv-window", {
        panel: { width: 280, height: 140, title: "MCV", layout: "row" },
    }, [
        hostApi.ui.panel("mcv-portrait", {
            panel: {
                width: 80, height: 80,
                background: { animation: portrait, fill: { width: "fit", height: "stretch", keepAspectRatio: true } },
            },
        }),
        hostApi.ui.panel("mcv-values", {
            panel: { width: 180, height: 110, layout: "column" },
        }, [
            hostApi.ui.panel("mcv-speed-row", { panel: { layout: "row" } }, [
                hostApi.ui.text("mcv-speed-label", "Movement Speed: "),
                hostApi.ui.field("mcv-speed-value", { entity: mcv, map: "number", name: "movementSpeed", fallback: "0" }),
            ]),
            hostApi.ui.panel("mcv-resource-row", { panel: { layout: "row" } }, [
                hostApi.ui.text("mcv-resource-label", "Resource: "),
                hostApi.ui.field("mcv-resource-value", { entity: mcv, map: "number", name: "resource", fallback: "0" }),
            ]),
            hostApi.ui.progress("mcv-resource", {
                panel: { width: 140, height: 24 },
                progress: {
                    fill: "assets/resource-fill.png",
                    track: "assets/resource-track.png",
                    entity: mcv,
                    min: () => number.of(0),
                    max: () => number.of(100),
                    value: (entity) => entity.number_map.get("resource").orElse(number.of(0)),
                },
            }),
            hostApi.ui.panel("mcv-interest-row", { panel: { layout: "row" } }, [
                hostApi.ui.text("mcv-interest-label", "Interest: "),
                hostApi.ui.field("mcv-interest-value", { entity: global, map: "number", name: "Interest", fallback: "0" }),
            ]),
        ]),
    ]);
};
export default entry;
