import { getLandModule } from "./land";
import { getLandSprites } from "./sprites";
import { getInventoryModule } from "../inventory/inventory";
export const landUi = (hostApi) => {
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 14, color: "#FFFFFF" });
    const module = getLandModule(hostApi);
    const inventory = getInventoryModule(hostApi);
    const track = hostApi.runtime.setAnimation(hostApi.runtime.string.of("land-track"), {
        frames: [{ sprite: hostApi.ui.getSpritePNG("black_pixel.png") }],
        duration: hostApi.runtime.number.of(1),
    });
    const portrait = hostApi.runtime.setAnimation(hostApi.runtime.string.of("land-portrait"), {
        frames: getLandSprites(hostApi).map(value => ({ sprite: value })),
        duration: hostApi.runtime.number.of(1),
        loop: true,
    });
    hostApi.ui.window("land-list", {
        panel: {
            x: 50,
            y: 250,
            width: 200,
            height: 400,
            title: "Lands",
            layout: "column",
            background: {
                animation: track,
                fill: { width: "stretch", height: "stretch" },
            },
        },
    }, [
        hostApi.ui.entityList("land-items", { container: module.container }, (entity) => [
            hostApi.ui.window(entity.id, {
                panel: {
                    width: 180,
                    height: 60,
                    layout: "row",
                },
            }, [
                hostApi.ui.panel(entity.id + ":portrait", {
                    panel: {
                        width: 100,
                        height: 60,
                        background: {
                            animation: portrait,
                            fill: {
                                width: "fit",
                                height: "stretch",
                                keepAspectRatio: true,
                            }
                        }
                    }
                }),
                hostApi.ui.panel(entity.id + ":stats", {
                    panel: {
                        width: 80,
                        height: 60,
                        layout: "column"
                    }
                }, [
                    hostApi.ui.field(entity.id + ":stats:name", {
                        entity: entity,
                        map: "text",
                        name: "name",
                        fallback: "Unknown",
                    }, font),
                    hostApi.ui.field(entity.id + ":stats:cost", {
                        entity: entity,
                        map: "number",
                        name: "cost",
                        fallback: "0",
                    }, font),
                ]),
                hostApi.ui.panel(entity.id + ":buy", {
                    panel: {
                        width: 50,
                        height: 60,
                        background: {
                            animation: track,
                            fill: { width: "stretch", height: "stretch" },
                        },
                        onClick: (ctx) => {
                            ctx.emitAction(inventory.buy, { entityId: Number(entity.id) });
                        },
                    },
                }, [
                    hostApi.ui.text(entity.id + ":buy:label", "Buy", font),
                ]),
            ]),
        ]),
    ]);
};
