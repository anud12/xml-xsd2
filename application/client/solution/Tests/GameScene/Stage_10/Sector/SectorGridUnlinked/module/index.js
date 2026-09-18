export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.world.sectorGrid(string.of("cave"));

  // A single-cell sector at [1,1] with openings on all four boundary edges.
  // No neighbouring sector exists, so none of these openings can link; every
  // one is an unlinked dead-end that renders as a red headless shaft on its
  // boundary edge (in contrast to a linked portal, which is orange).
  hostApi.runtime.setContainer(string.of("room-a"), {
    entities: [],
    sector: {
      grid: string.of("cave"),
      footprint: [[0, 0]],
      at: [1, 1],
      openings: [
        { cell: [0, 0], side: "N", start: number.of(0), length: number.of(1) },
        { cell: [0, 0], side: "E", start: number.of(0), length: number.of(1) },
        { cell: [0, 0], side: "S", start: number.of(0), length: number.of(1) },
        { cell: [0, 0], side: "W", start: number.of(0), length: number.of(1) },
      ],
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

  // A top-down view of the grid. The lone cell (1,1) sits at local 44..76;
  // its four unlinked openings render as shafts on the cell's boundary edges.
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
