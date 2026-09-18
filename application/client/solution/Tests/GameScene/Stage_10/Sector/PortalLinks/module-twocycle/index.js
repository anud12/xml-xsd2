/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.world.sectorGrid(hostApi.runtime.string.of("cave"));
  // Concave L: (0,0)(1,0) / (0,1). The pocket square (1,1) is faced by both
  // (1,0) S and (0,1) E -> a 2-cycle when a sector fills the pocket.
  hostApi.runtime.setContainer(hostApi.runtime.string.of("room-a"), {
    entities: [],
    sector: {
      grid: hostApi.runtime.string.of("cave"),
      footprint: [[0, 0], [1, 0], [0, 1]],
      at: [0, 0],
      openings: [
        { cell: [1, 0], side: "S", start: hostApi.runtime.number.of(0), length: hostApi.runtime.number.of(4) },
        { cell: [0, 1], side: "E", start: hostApi.runtime.number.of(0), length: hostApi.runtime.number.of(4) },
      ],
    },
  });
  // Pocket filler at (1,1).
  hostApi.runtime.setContainer(hostApi.runtime.string.of("room-b"), {
    entities: [],
    sector: {
      grid: hostApi.runtime.string.of("cave"),
      footprint: [[0, 0]],
      at: [1, 1],
      openings: [
        { cell: [0, 0], side: "N", start: hostApi.runtime.number.of(0), length: hostApi.runtime.number.of(4) },
        { cell: [0, 0], side: "W", start: hostApi.runtime.number.of(0), length: hostApi.runtime.number.of(4) },
      ],
    },
  });
};
