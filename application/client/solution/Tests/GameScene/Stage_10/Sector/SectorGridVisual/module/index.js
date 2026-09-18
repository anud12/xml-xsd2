/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.world.sectorGrid(string.of("cave"));

  // An L-shaped sector occupying (0,0),(1,0),(0,1). Its (1,0) arm opens east
  // toward the box sector's west edge.
  hostApi.runtime.setContainer(string.of("room-l"), {
    entities: [],
    sector: {
      grid: string.of("cave"),
      footprint: [[0, 0], [1, 0], [0, 1]],
      at: [0, 0],
      openings: [
        { cell: [1, 0], side: "E", start: number.of(0), length: number.of(1) },
      ],
    },
  });

  // A 1x1 sector at (2,0), directly east of the L's (1,0) arm.
  hostApi.runtime.setContainer(string.of("room-b"), {
    entities: [],
    sector: {
      grid: string.of("cave"),
      footprint: [[0, 0]],
      at: [2, 0],
      openings: [
        { cell: [0, 0], side: "W", start: number.of(0), length: number.of(1) },
      ],
    },
  });

  // Solid-color textures for the view background and the cells. Portals are
  // drawn by the engine as headless arrows (a solid shaft), not by the module.
  hostApi.runtime.registerAnimation(string.of("viewbg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("viewbg.png") }],
    duration: number.of(1),
  });
  hostApi.runtime.registerAnimation(string.of("cell"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("cell.png") }],
    duration: number.of(1),
  });

  // A top-down view of the grid: 3 columns x 2 rows at 40px cells with an 8px
  // gap between cells (each cell inset 4px on every side). The view carries its
  // own background; the render is invoked once per cell and returns the node to
  // place. Portals are rendered by the engine itself (headless arrows).
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
