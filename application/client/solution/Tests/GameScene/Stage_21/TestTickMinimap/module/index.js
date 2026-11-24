export default (hostApi) => {
  const { number, string } = hostApi.runtime;
  const tile = hostApi.runtime.setAnimation(string.of("minimal-tile"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("tile.png") }], duration: number.of(1),
  });
  const marker = hostApi.runtime.setAnimation(string.of("minimal-marker"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("marker.png") }], duration: number.of(1),
  });
  const fill = { width: "stretch", height: "stretch", keepAspectRatio: false };
  const mcv = hostApi.runtime.setEntity({
    id: string.of("mcv"),
    numberMap: { movementSpeed: 10, resource: 70, x: 0, y: 0 },
    stringMap: { xKey: "x", yKey: "y" },
  });
  const map = hostApi.runtime.setEntity({
    id: string.of("mcv-map"),
    entityListMap: { members: [mcv] },
    numberMap: { sizeX: 10, sizeY: 10 },
    stringMap: { outOfBoundsX: "clamp", outOfBoundsY: "clamp" },
  });
  const moveMcv = hostApi.runtime.setAction({
    name: string.of("move-mcv-to-cell"),
    apply: ctx => ctx.teleportTo({ groupId: map, entityId: mcv, x: ctx.args.x, y: ctx.args.y, clamp: true }),
  });

  hostApi.ui.setScreen({ screen: { anchorChild: "center" } }, [
    hostApi.ui.groupView("mcv-map-view", {
      group: map,
      list: "members",
      emptyCell: { animation: tile, fill },
      panel: {
        width: 800, height: 600, layout: { group: map },
        onHover: {
          window: ctx => {
            const entity = globalThis.__readEntity("mcv");
            if (!entity || !ctx.cell || ctx.cell.x !== entity.numberMap.x || ctx.cell.y !== entity.numberMap.y) return null;
            return {
              options: { panel: {
                width: 190, height: 110, title: "Stats", layout: "column",
                pinnable: { key: "P" },
                position: { cursor: { offset: { x: 12, y: 12 } } },
              } },
              children: [
                hostApi.ui.panel("stats-x-row", { panel: { layout: "row" } }, [
                  hostApi.ui.text("stats-x-label", "X: "),
                  hostApi.ui.field("stats-x", { entity: mcv, map: "number", name: "x", fallback: "0" }),
                ]),
                hostApi.ui.panel("stats-y-row", { panel: { layout: "row" } }, [
                  hostApi.ui.text("stats-y-label", "Y: "),
                  hostApi.ui.field("stats-y", { entity: mcv, map: "number", name: "y", fallback: "0" }),
                ]),
                hostApi.ui.panel("stats-value-row", { panel: { layout: "row" } }, [
                  hostApi.ui.text("stats-value-label", "Value: "),
                  hostApi.ui.field("stats-value", { entity: mcv, map: "number", name: "resource", fallback: "0" }),
                ]),
              ],
            };
          },
        },
        onClick: ctx => {
          if (ctx.cursor) ctx.emitAction(moveMcv, { x: ctx.cursor.getX(), y: ctx.cursor.getY() });
        },
      },
    }, item => hostApi.ui.window(item.id + "-map-cell", { panel: {} }, [
      hostApi.ui.panel(item.id + "-map-marker", { panel: { width: 32, height: 32, background: { animation: marker, fill } } }),
    ])),
  ]);
};
