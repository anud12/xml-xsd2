/** @type {ModuleEntrypoint} */
export default (hostApi) => {
    const { string } = hostApi.runtime;

    const itemA = hostApi.runtime.setEntity({ id: string.of("item-a") });
    const itemB = hostApi.runtime.setEntity({ id: string.of("item-b") });
    const itemC = hostApi.runtime.setEntity({ id: string.of("item-c") });
    const inv = hostApi.runtime.setContainer({
        id: string.of("inventory"),
        entities: [itemA, itemB, itemC],
    });

    // A horizontal flow list: vertical: false drives a left-to-right row.
    hostApi.ui.window("row-host", {
        panel: { x: 40, y: 40, width: 400, height: 60 },
    }, [
        hostApi.ui.entityList("row-list", { container: inv, vertical: false }, (entity) => [
            hostApi.ui.window(entity.id + "-r", { panel: { width: 40, height: 40 } }),
        ]),
    ]);

    // A vertical flow list: the default (no vertical) drives a top-to-bottom column.
    hostApi.ui.window("col-host", {
        panel: { x: 40, y: 120, width: 60, height: 200 },
    }, [
        hostApi.ui.entityList("col-list", { container: inv }, (entity) => [
            hostApi.ui.window(entity.id + "-c", { panel: { width: 40, height: 40 } }),
        ]),
    ]);
};
