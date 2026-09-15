export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // Each container holds one 2x2-span entity at top-left cell (2,1). The two
  // containers are identical except for the alignment flag, so the marker
  // position difference isolates the centering behavior.
  hostApi.runtime.setEntity(string.of("node-a"), {
    numberMap: { column: number.of(2), row: number.of(1), span: number.of(2) },
  });
  hostApi.runtime.setEntity(string.of("node-b"), {
    numberMap: { column: number.of(2), row: number.of(1), span: number.of(2) },
  });

  // grid-center declares alignment "center": the marker is centered on the
  // midpoint of its span.
  hostApi.runtime.setContainer(string.of("grid-center"), {
    entities: [string.of("node-a")],
    getX: (entity) => entity.number_map.get("column").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("row").orElse(number.of(0)),
    getSpanX: (entity) => entity.number_map.get("span").orElse(number.of(1)),
    getSpanY: (entity) => entity.number_map.get("span").orElse(number.of(1)),
    sizeX: { value: number.of(10), outOfBounds: "clamp" },
    sizeY: { value: number.of(5), outOfBounds: "clamp" },
    alignment: "center",
  });

  // grid-default omits the alignment flag: the marker pins its top-left
  // corner to (x, y) (the default "top-left" behavior).
  hostApi.runtime.setContainer(string.of("grid-default"), {
    entities: [string.of("node-b")],
    getX: (entity) => entity.number_map.get("column").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("row").orElse(number.of(0)),
    getSpanX: (entity) => entity.number_map.get("span").orElse(number.of(1)),
    getSpanY: (entity) => entity.number_map.get("span").orElse(number.of(1)),
    sizeX: { value: number.of(10), outOfBounds: "clamp" },
    sizeY: { value: number.of(5), outOfBounds: "clamp" },
  });

  // Each 700x500 view maps its 10x5 container cell-for-cell: each cell is
  // 70 wide and 100 tall.
  hostApi.ui.containerView("plane-center", {
    container: "grid-center",
    width: 700,
    height: 500,
    x: 10,
    y: 80,
  }, entity => hostApi.ui.window(entity.id, {}, [
    hostApi.ui.panel(entity.id + "-marker", { width: 140, height: 200 }),
  ]));

  hostApi.ui.containerView("plane-default", {
    container: "grid-default",
    width: 700,
    height: 500,
    x: 10,
    y: 600,
  }, entity => hostApi.ui.window(entity.id, {}, [
    hostApi.ui.panel(entity.id + "-marker", { width: 140, height: 200 }),
  ]));
}
