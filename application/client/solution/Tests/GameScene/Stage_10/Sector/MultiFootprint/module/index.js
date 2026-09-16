export default (hostApi) => {
  hostApi.world.sectorGrid(hostApi.runtime.string.of("cave"));
  hostApi.runtime.setContainer(hostApi.runtime.string.of("room-l"), {
    entities: [],
    sector: {
      grid: hostApi.runtime.string.of("cave"),
      // L footprint: (0,0)(1,0) / (0,1)
      footprint: [[0, 0], [1, 0], [0, 1]],
      at: [0, 0],
      openings: [],
    },
  });
};
