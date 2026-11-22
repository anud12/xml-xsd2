export default (hostApi) => {
    const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });
 const { number, string } = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("node-1"),
    numberMap: { column: number.of(2), row: number.of(1) },
    stringMap: { xKey: "column", yKey: "row" },
  });

  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("node-2"),
    numberMap: { column: number.of(0), row: number.of(0) },
    stringMap: { xKey: "column", yKey: "row" },
  });

  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("grid-1"),
    entityListMap: { members: [
      hostApi.runtime.string.of("node-1"),
      hostApi.runtime.string.of("node-2"),
    ] },
    numberMap: { sizeX: number.of(10), sizeY: number.of(5) },
    stringMap: { outOfBoundsX: "clamp", outOfBoundsY: "clamp" },
  });

  hostApi.runtime.setAction({
    name: string.of("relocate-node-1"),
    apply: (ctx) => {
      ctx.teleportTo({
        groupId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: 6,
        y: 3,
        clamp: true,
      });
    },
  });

  hostApi.runtime.setAction({
    name: string.of("relocate-node-1-out-of-bounds"),
    apply: (ctx) => {
      ctx.teleportTo({
        groupId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: 15,
        y: 9,
        clamp: true,
      });
    },
  });

  // Readouts bind node-1's column/row number values: the labels re-resolve
  // from the entity store every frame, so they follow the teleports.
  hostApi.ui.window("col", {
    panel: {
      width: 80,
      height: 40,
      x: 10,
      y: 10,
    },
  }, [
    hostApi.ui.field("col-value", { entity: "node-1", map: "number", name: "column", fallback: "0" }, font),
  ]);

  hostApi.ui.window("row", {
    panel: {
      width: 80,
      height: 40,
      x: 100,
      y: 10,
    },
  }, [
    hostApi.ui.field("row-value", { entity: "node-1", map: "number", name: "row", fallback: "0" }, font),
  ]);
}