export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.world.sectorGrid(string.of("cave"));

  // Sector A: a single 1x1 cell at [0,0]. It opens on its RIGHT (east) edge.
  hostApi.runtime.setContainer(string.of("room-a"), {
    entities: [],
    sector: {
      grid: string.of("cave"),
      footprint: [[0, 0]],
      at: [0, 0],
      openings: [
        { cell: [0, 0], side: "E", start: number.of(0), length: number.of(1) },
      ],
    },
  });

  // Sector B: a single 1x1 cell at [2,2] — diagonally opposite A (two cells right,
  // two cells down), with blank squares between. It opens on its LEFT (west) edge.
  // A's east opening and B's west opening are NOT facing (they sit on different
  // rows), so with no explicit link both are unlinked dead-ends and no portal forms.
  hostApi.runtime.setContainer(string.of("room-b"), {
    entities: [],
    sector: {
      grid: string.of("cave"),
      footprint: [[0, 0]],
      at: [2, 2],
      openings: [
        { cell: [0, 0], side: "W", start: number.of(0), length: number.of(1) },
      ],
    },
  });

  // The action the test fires: it delegates to an effect.
  hostApi.runtime.registerAction({
    name: string.of("link-ab"),
    apply: (ctx) => { ctx.emitEffect("do-link", {}); },
  });

  // The effect: links the two diagonal openings — A's east opening (0,0) and B's
  // west opening (global (2,2)). Because they do not face each other, the engine
  // must route a Manhattan (orthogonal) corridor between them through the gaps.
  hostApi.runtime.registerEffect({
    name: "do-link",
    apply: () => {
      hostApi.runtime.linkOpening(
        { container: "room-a", cell: [0, 0], side: "E" },
        { container: "room-b", cell: [0, 0], side: "W" }
      );
    },
  });

  hostApi.runtime.registerAnimation(string.of("viewbg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("viewbg.png") }],
    duration: number.of(1),
  });
  hostApi.runtime.registerAnimation(string.of("cell"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("cell.png") }],
    duration: number.of(1),
  });

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
