export default (hostApi) => {
  hostApi.world.sectorGrid(hostApi.runtime.string.of("cave"));
  hostApi.runtime.setContainer(hostApi.runtime.string.of("room-a"), {
    entities: [],
    sector: {
      grid: hostApi.runtime.string.of("cave"),
      footprint: [[0, 0]],
      at: [0, 0],
      openings: [
        { cell: [0, 0], side: "E", start: hostApi.runtime.number.of(0), length: hostApi.runtime.number.of(4) },
      ],
    },
  });
  hostApi.runtime.setContainer(hostApi.runtime.string.of("room-b"), {
    entities: [],
    sector: {
      grid: hostApi.runtime.string.of("cave"),
      footprint: [[0, 0]],
      at: [1, 0],
      openings: [],
    },
  });
};
