export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // Each container holds one 2x2-span entity at top-left cell (2,1). The two
  // containers are identical except for the alignment flag, so the marker
  // position difference isolates the centering behavior.
  hostApi.runtime.setEntity({
    id: string.of("node-a"),
    numberMap: { column: number.of(2), row: number.of(1), span: number.of(2), spanX: number.of(2), spanY: number.of(2) },
    stringMap: { xKey: "column", yKey: "row" },
  });
  hostApi.runtime.setEntity({
    id: string.of("node-b"),
    numberMap: { column: number.of(2), row: number.of(1), span: number.of(2), spanX: number.of(2), spanY: number.of(2) },
    stringMap: { xKey: "column", yKey: "row" },
  });

  // grid-center declares alignment "center": the marker is centered on the
  // midpoint of its span.
  hostApi.runtime.setEntity({
    id: string.of("grid-center"),
    entityListMap: { members: [string.of("node-a")] },
    numberMap: { sizeX: number.of(10), sizeY: number.of(5) },
    stringMap: { outOfBoundsX: "clamp", outOfBoundsY: "clamp", alignment: "center" },
  });

  // grid-default omits the alignment flag: the marker pins its top-left
  // corner to (x, y) (the default "top-left" behavior).
  hostApi.runtime.setEntity({
    id: string.of("grid-default"),
    entityListMap: { members: [string.of("node-b")] },
    numberMap: { sizeX: number.of(10), sizeY: number.of(5) },
    stringMap: { outOfBoundsX: "clamp", outOfBoundsY: "clamp" },
  });

  // Each 700x500 view maps its 10x5 container cell-for-cell: each cell is
  // 70 wide and 100 tall.
  hostApi.ui.window("plane-center-host", {
    panel: {
      x: 10,
      y: 80,
      width: 700,
      height: 500,
    },
  }, [
    hostApi.ui.groupView("plane-center", {
      group: "grid-center", list: "members",
      panel: {
        width: 700,
        height: 500,
      },
    }, entity => hostApi.ui.window(entity.id, {}, [
      hostApi.ui.window(entity.id + "-marker", { panel: { width: 140, height: 200 } }),
    ]))
  ]);

  hostApi.ui.window("plane-default-host", {
    panel: {
      x: 10,
      y: 600,
      width: 700,
      height: 500,
    },
  }, [
    hostApi.ui.groupView("plane-default", {
      group: "grid-default", list: "members",
      panel: {
        width: 700,
        height: 500,
      },
    }, entity => hostApi.ui.window(entity.id, {}, [
      hostApi.ui.window(entity.id + "-marker", { panel: { width: 140, height: 200 } }),
    ]))
  ]);
}
