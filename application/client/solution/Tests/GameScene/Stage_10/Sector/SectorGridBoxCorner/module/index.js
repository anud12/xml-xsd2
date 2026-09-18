/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.world.sectorGrid(string.of("cave"));

  // An L-shaped sector at [0,0] over cells (0,0),(1,0),(0,1). Its (1,0) arm opens
  // south toward the box's (1,1) north edge, and its (0,1) cell opens east toward
  // the box's (1,1) west edge.
  hostApi.runtime.setContainer(string.of("room-l"), {
    entities: [],
    sector: {
      grid: string.of("cave"),
      footprint: [[0, 0], [1, 0], [0, 1]],
      at: [0, 0],
      openings: [
        { cell: [1, 0], side: "S", start: number.of(0), length: number.of(1) },
        { cell: [0, 1], side: "E", start: number.of(0), length: number.of(1) },
      ],
    },
  });

  // A 1x1 "box" sector at [1,1]: opens north (to L's (1,0) S) and west (to L's
  // (0,1) E), forming two portals with the L sector.
  hostApi.runtime.setContainer(string.of("room-b"), {
    entities: [],
    sector: {
      grid: string.of("cave"),
      footprint: [[0, 0]],
      at: [1, 1],
      openings: [
        { cell: [0, 0], side: "N", start: number.of(0), length: number.of(1) },
        { cell: [0, 0], side: "W", start: number.of(0), length: number.of(1) },
      ],
    },
  });

  // Solid-color textures for the view background and the cells. Portals are
  // drawn by the engine as headless shafts, not by the module.
  hostApi.runtime.registerAnimation(string.of("viewbg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("viewbg.png") }],
    duration: number.of(1),
  });
  hostApi.runtime.registerAnimation(string.of("cell"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("cell.png") }],
    duration: number.of(1),
  });

  // A top-down view of the 2x2 grid: 40px cells with an 8px gap. The render is
  // invoked once per cell and returns the node to place; portals are rendered by
  // the engine itself (headless shafts).
  hostApi.ui.sectorGrid("caveview", {
    grid: "cave",
    cellSize: 40,
    cellGap: 8,
    width: 700,
    height: 400,
    x: 200,
    y: 200,
    background: hostApi.ui.getAnimation(string.of("viewbg")),
  }, (item) => hostApi.ui.window(item.id, {
    background: hostApi.ui.getAnimation(string.of("cell")),
  }));
};
