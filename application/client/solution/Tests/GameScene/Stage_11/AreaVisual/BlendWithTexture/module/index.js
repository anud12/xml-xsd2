/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // Register the item's sprite texture so the container-view item can paint
  // it as its background (a solid bright yellow 10x10 png).
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("item"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("item.png") },
    ],
    duration: number.of(1),
  });

  // A room at (10, 10) declaring a single area. The view (700x500 over a
  // 100x100 container, cell_w = 7, cell_h = 5) stamps the item at
  // view-local (70, 50). The area box (30 x 20) covers only the TOP half of
  // the 30x40 item texture, so the bottom of the texture stays uncovered:
  // the outline+body blend over the top, the bare yellow texture shows below.
  hostApi.runtime.setEntity(string.of("room"), {
    numberMap: { column: number.of(10), row: number.of(10) },
    areaMap: {
      floor: {
        polygon: [[0, 0], [30, 0], [30, 20], [0, 20]],
      },
    },
  });

  hostApi.runtime.setContainer(string.of("grid-1"), {
    entities: [string.of("room")],
    getX: (entity) => entity.number_map.get("column").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("row").orElse(number.of(0)),
    // Give the item a 30x40 span so its yellow texture stretches across the
    // whole area polygon (view-local (70,50)..(280,250)). Without a span the
    // item is 1x1 (7x5 px) and the outline would not overlap the texture.
    getSpanX: (entity) => number.of(30),
    getSpanY: (entity) => number.of(40),
    sizeX: { value: number.of(100), outOfBounds: "clamp" },
    sizeY: { value: number.of(100), outOfBounds: "clamp" },
  });

  // A 700x500 top-down view (cell_w = 7, cell_h = 5). Selects the "floor"
  // area with a red outline + green body. The item paints its yellow texture
  // as the background; the outline must render OVER that texture, so the
  // red outline edge must beat the yellow texture at the edge pixels.
  hostApi.ui.containerView("world", {
    container: "grid-1",
    width: 700,
    height: 500,
    area: [
      { name: "floor", color: [1, 0, 0, 0.5], bodyColor: [0, 1, 0, 0.5], thickness: 3 },
    ],
  }, (entity) => hostApi.ui.window(entity.id, {
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("item")),
  }, []));
};
