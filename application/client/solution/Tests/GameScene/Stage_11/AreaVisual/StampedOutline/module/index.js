/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // A "room" that declares a local area shaped as a 5-point polygon: a 60x80
  // box with the top-right corner cut off (the diagonal [40,0] -> [60,20]).
  // Container world position is (10, 10). In the 700x500 view (cell 7x5) the
  // polygon maps to view-local (70,50)..(490,450) — fully inside the view.
  hostApi.runtime.setEntity(string.of("room"), {
    numberMap: { column: number.of(10), row: number.of(10) },
    areaMap: {
      floor: {
        polygon: [[0, 0], [40, 0], [60, 20], [60, 80], [0, 80]],
      },
    },
  });

  // A "crate" with no declared area (the 1x1 fallback case).
  hostApi.runtime.setEntity(string.of("crate"), {
    numberMap: { column: number.of(40), row: number.of(40) },
  });

  hostApi.runtime.setContainer(string.of("grid-1"), {
    entities: [string.of("room"), string.of("crate")],
    getX: (entity) => entity.number_map.get("column").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("row").orElse(number.of(0)),
    sizeX: { value: number.of(100), outOfBounds: "clamp" },
    sizeY: { value: number.of(100), outOfBounds: "clamp" },
  });

  // A 700x500 top-down view of the 100x100 container: cell_w = 7, cell_h = 5.
  // Each item is stamped with its entity's area polygon in view-local units.
  hostApi.ui.containerView("world", {
    container: "grid-1",
    width: 700,
    height: 500,
    area: [
      { name: "floor", color: [1, 0, 0, 1], bodyColor: [0, 1, 0, 1], thickness: 3 },
    ],
  }, (entity) => hostApi.ui.window(entity.id, {}, []));
};
