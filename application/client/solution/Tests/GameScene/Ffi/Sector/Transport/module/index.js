export default (hostApi) => {
  hostApi.world.sectorGrid(hostApi.runtime.string.of("cave"));
  hostApi.runtime.setContainer({
    id: hostApi.runtime.string.of("room-a"),
    entities: [],
    sector: {
      grid: hostApi.runtime.string.of("cave"),
      footprint: [[0, 0]],
      at: [0, 0],
      openings: [],
    },
  });
};
