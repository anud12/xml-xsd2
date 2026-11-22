/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // A "room" that declares TWO named areas so the view can draw both at once.
  // Container world position is (10, 10). In the 700x500 view (cell_w = 7,
  // cell_h = 5) each local point (px, py) maps to view-local (px*7+70, py*5+50).
  const room = hostApi.runtime.setEntity({
    id: string.of("room"),
    numberMap: { column: number.of(10), row: number.of(10) },
    stringMap: { xKey: "column", yKey: "row" },
    polygons: { floor: [[0, 0], [30, 0], [30, 40], [0, 40]], roof: [[0, 40], [30, 40], [30, 80], [0, 80]] },
  });

  const grid1 = hostApi.runtime.setEntity({
    id: string.of("grid-1"),
    entityListMap: { members: [room] },
    numberMap: { sizeX: number.of(100), sizeY: number.of(100) },
    stringMap: { outOfBoundsX: "clamp", outOfBoundsY: "clamp" },
  });

  // A 700x500 top-down view of the 100x100 container (cell_w = 7, cell_h = 5).
  // Two areas are selected, each with its own outline style:
  //   floor -> red outline (1,0,0), green body (0,1,0), thickness 3
  //   roof  -> blue outline (0,0,1), cyan body (0,1,1), thickness 4
  hostApi.ui.window("world-host", {
    panel: {
      x: 0,
      y: 0,
      width: 700,
      height: 500,
    },
  }, [
    hostApi.ui.groupView("world", {
      group: grid1, list: "members",
      panel: {
        width: 700,
        height: 500,
      },
      area: [
        { name: "floor", color: [1, 0, 0, 1], bodyColor: [0, 1, 0, 1], thickness: 3 },
        { name: "roof", color: [0, 0, 1, 1], bodyColor: [0, 1, 1, 1], thickness: 4 },
      ],
    }, (entity) => hostApi.ui.window(entity.id, { panel: {} }, []))
  ]);
};
