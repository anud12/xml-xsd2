/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  const cave = hostApi.world.sectorGrid(string.of("cave"));

  // Sector A: a single 1x1 cell at [0,0]. It opens on its RIGHT (east) edge.
  const roomA = hostApi.runtime.setContainer({
    id: string.of("room-a"),
    entities: [],
    sector: {
      grid: cave,
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
  const roomB = hostApi.runtime.setContainer({
    id: string.of("room-b"),
    entities: [],
    sector: {
      grid: cave,
      footprint: [[0, 0]],
      at: [2, 2],
      openings: [
        { cell: [0, 0], side: "W", start: number.of(0), length: number.of(1) },
      ],
    },
  });

  // The effect: links the two diagonal openings — A's east opening (0,0) and B's
  // west opening (global (2,2)). Because they do not face each other, the engine
  // must route a Manhattan (orthogonal) corridor between them through the gaps.
  const doLink = hostApi.runtime.setEffect({
    name: string.of("do-link"),
    apply: () => {
      hostApi.runtime.linkOpening(
         { container: roomA, cell: [0, 0], side: "E" },
         { container: roomB, cell: [0, 0], side: "W" }
        );
    },
  });

  // The action the test fires: it delegates to an effect.
  hostApi.runtime.setAction({
    name: string.of("link-ab"),
    apply: (ctx) => { ctx.emitEffect(doLink, {}); },
  });

  const viewbg = hostApi.runtime.setAnimation(string.of("viewbg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("viewbg.png") }],
    duration: number.of(1),
  });
  const cell = hostApi.runtime.setAnimation(string.of("cell"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("cell.png") }],
    duration: number.of(1),
  });

  hostApi.ui.sectorGrid("caveview", {
    grid: cave,
    cellSize: 40,
    cellGap: 8,
    panel: {
      width: 700,
      height: 400,
      x: 200,
      y: 200,
      background: viewbg,
    },
  }, (item) => hostApi.ui.window(item.id, {
    panel: {
      background: cell,
    },
  }));
};
