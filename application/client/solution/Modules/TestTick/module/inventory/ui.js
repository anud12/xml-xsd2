import { getInventoryModule } from "./inventory";
import { getLandSprites } from "../land/sprites";
export const inventoryUi = (hostApi) => {
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 14, color: "#FFFFFF" });
    const module = getInventoryModule(hostApi);
    const track = hostApi.runtime.setAnimation(hostApi.runtime.string.of("inventory-track"), {
        frames: [{ sprite: hostApi.ui.getSpritePNG("assets/black_pixel.png") }],
        duration: hostApi.runtime.number.of(1),
    });
    const portrait = hostApi.runtime.setAnimation(hostApi.runtime.string.of("inventory-portrait"), {
        frames: getLandSprites(hostApi).map(value => ({ sprite: value })),
        duration: hostApi.runtime.number.of(1),
        loop: true,
    });
    hostApi.ui.window("inventory-list", {
        panel: {
            x: 50,
            y: 100,
            width: 300,
            height: 100,
            title: "Inventory",
            layout: "row",
            overflow: {
                width: "scroll",
                height: "show",
            },
            background: {
                animation: track,
                fill: { width: "stretch", height: "stretch" },
            },
        },
    }, [
        hostApi.ui.entityList("inventory-items", {
            container: module.container,
            vertical: false,
            panel: {
                width: 300,
                height: 100,
            }
        }, (entity) => [
            hostApi.ui.window(entity.id, {
                panel: {
                    width: 120,
                    height: 100,
                    layout: "column",
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
                        width: 100,
                        height: 30,
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
                ])
            ]),
        ]),
    ]);
};
