export default (hostApi) => {
  hostApi.world.sectorGrid(hostApi.runtime.string.of("cave"));
  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("room-a"),
    entityListMap: { members: [] },
    sector: {
      grid: hostApi.runtime.string.of("cave"),
      footprint: [[0, 0]],
      at: [0, 0],
      openings: [],
    },
  });
};
