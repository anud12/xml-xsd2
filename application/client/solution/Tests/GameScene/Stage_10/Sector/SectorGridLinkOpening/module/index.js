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

  // Sector B: a single 1x1 cell at [2,0] — one blank square east of A. It opens
  // on its LEFT (west) edge. With no explicit link, both openings are unlinked
  // dead-ends: nothing connects A and B by adjacency (a blank square sits between).
  const roomB = hostApi.runtime.setContainer({
    id: string.of("room-b"),
    entities: [],
    sector: {
      grid: cave,
      footprint: [[0, 0]],
      at: [2, 0],
      openings: [
        { cell: [0, 0], side: "W", start: number.of(0), length: number.of(1) },
      ],
    },
  });

  // The effect: calls linkOpening, passing the two openings to link — A's east
  // opening and B's west opening. Each endpoint names its container, its local
  // cell, and its boundary side.
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

  hostApi.runtime.setAnimation(string.of("viewbg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("viewbg.png") }],
    duration: number.of(1),
  });
  hostApi.runtime.setAnimation(string.of("cell"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("cell.png") }],
    duration: number.of(1),
  });

  hostApi.ui.sectorGrid("caveview", {
    grid: cave,
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
