/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // A "room" that declares TWO named areas so the view can draw both at once.
  // Container world position is (10, 10). In the 700x500 view (cell_w = 7,
  // cell_h = 5) each local point (px, py) maps to view-local (px*7+70, py*5+50).
  hostApi.runtime.setEntity(string.of("room"), {
    numberMap: { column: number.of(10), row: number.of(10) },
    areaMap: {
      floor: {
        polygon: [[0, 0], [30, 0], [30, 40], [0, 40]],
      },
      roof: {
        polygon: [[0, 40], [30, 40], [30, 80], [0, 80]],
      },
    },
  });

  hostApi.runtime.setContainer(string.of("grid-1"), {
    entities: [string.of("room")],
    getX: (entity) => entity.number_map.get("column").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("row").orElse(number.of(0)),
    sizeX: { value: number.of(100), outOfBounds: "clamp" },
    sizeY: { value: number.of(100), outOfBounds: "clamp" },
  });

  // A 700x500 top-down view of the 100x100 container (cell_w = 7, cell_h = 5).
  // Two areas are selected, each with its own outline style:
  //   floor -> red outline (1,0,0), green body (0,1,0), thickness 3
  //   roof  -> blue outline (0,0,1), cyan body (0,1,1), thickness 4
  hostApi.ui.containerView("world", {
    container: "grid-1",
    width: 700,
    height: 500,
    area: [
      { name: "floor", color: [1, 0, 0, 1], bodyColor: [0, 1, 0, 1], thickness: 3 },
      { name: "roof", color: [0, 0, 1, 1], bodyColor: [0, 1, 1, 1], thickness: 4 },
    ],
  }, (entity) => hostApi.ui.window(entity.id, {}, []));
};
