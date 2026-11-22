/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // Register the item's sprite texture so the container-view item can paint
  // it as its background (a solid bright yellow 10x10 png).
  const item = hostApi.runtime.setAnimation(hostApi.runtime.string.of("item"), {
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
  const room = hostApi.runtime.setEntity({
    id: string.of("room"),
    numberMap: { column: number.of(10), row: number.of(10), spanX: number.of(30), spanY: number.of(40) },
    stringMap: { xKey: "column", yKey: "row" },
    polygons: { floor: [[0, 0], [30, 0], [30, 20], [0, 20]] },
  });

  const grid1 = hostApi.runtime.setEntity({
    id: string.of("grid-1"),
    entityListMap: { members: [room] },
    numberMap: { sizeX: number.of(100), sizeY: number.of(100) },
    stringMap: { outOfBoundsX: "clamp", outOfBoundsY: "clamp" },
  });

  // A 700x500 top-down view (cell_w = 7, cell_h = 5). Selects the "floor"
  // area with a red outline + green body. The item paints its yellow texture
  // as the background; the outline must render OVER that texture, so the
  // red outline edge must beat the yellow texture at the edge pixels.
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
        { name: "floor", color: [1, 0, 0, 0.5], bodyColor: [0, 1, 0, 0.5], thickness: 3 },
      ],
    }, (entity) => hostApi.ui.window(entity.id, {
      panel: {
        background: { animation: item, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
      },
    }, []))
  ]);
};
