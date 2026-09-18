/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.world.sectorGrid(string.of("cave"));

  // An L-shaped sector at [0,0] over cells (0,0),(1,0),(0,1). Its (1,0) arm has
  // an opening on the south edge, facing the (currently empty) square (1,1).
  // With no sector there yet, that opening is unlinked: a dead-end doorway
  // rendered as a red shaft on the L's boundary edge.
  hostApi.runtime.setContainer(string.of("room-l"), {
    entities: [],
    sector: {
      grid: string.of("cave"),
      footprint: [[0, 0], [1, 0], [0, 1]],
      at: [0, 0],
      openings: [
        { cell: [1, 0], side: "S", start: number.of(0), length: number.of(1) },
      ],
    },
  });

  // The action the test fires: it does nothing itself, it delegates to an
  // effect (an action -> effect indirection).
  hostApi.runtime.registerAction({
    name: string.of("build-room"),
    apply: (ctx) => {
      ctx.emitEffect("create-room", {});
    },
  });

  // The effect: creates a brand-new single-cell sector "room-b" at (1,1),
  // directly south of the L's (1,0) arm, with a north opening that faces the
  // L's south opening. This links the two sectors and forms a portal, and the
  // L's previously-unlinked doorway resolves into that linked portal.
  hostApi.runtime.registerEffect({
    name: "create-room",
    apply: () => {
      hostApi.runtime.setContainer(string.of("room-b"), {
        entities: [],
        sector: {
          grid: string.of("cave"),
          footprint: [[0, 0]],
          at: [1, 1],
          openings: [
            { cell: [0, 0], side: "N", start: number.of(0), length: number.of(1) },
          ],
        },
      });
    },
  });

  // Solid-color textures for the view background and the cells.
  hostApi.runtime.registerAnimation(string.of("viewbg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("viewbg.png") }],
    duration: number.of(1),
  });
  hostApi.runtime.registerAnimation(string.of("cell"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("cell.png") }],
    duration: number.of(1),
  });

  // A top-down view of the grid. The L occupies (0,0),(1,0),(0,1); the
  // dynamically created room-b appears at (1,1) after the action fires.
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
