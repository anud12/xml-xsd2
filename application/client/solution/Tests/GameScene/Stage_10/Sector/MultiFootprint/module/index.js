/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const cave = hostApi.world.sectorGrid(hostApi.runtime.string.of("cave"));
  hostApi.runtime.setContainer({
    id: hostApi.runtime.string.of("room-l"),
    entities: [],
    sector: {
      grid: cave,
      // L footprint: (0,0)(1,0) / (0,1)
      footprint: [[0, 0], [1, 0], [0, 1]],
      at: [0, 0],
      openings: [],
    },
  });
};
