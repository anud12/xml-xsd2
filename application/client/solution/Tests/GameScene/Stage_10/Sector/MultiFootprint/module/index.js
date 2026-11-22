/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const cave = hostApi.world.sectorGrid(hostApi.runtime.string.of("cave"));
  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("room-l"),
    entityListMap: { members: [] },
    sector: {
      grid: cave,
      // L footprint: (0,0)(1,0) / (0,1)
      footprint: [[0, 0], [1, 0], [0, 1]],
      at: [0, 0],
      openings: [],
    },
  });
};
