/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const cave = hostApi.world.sectorGrid(hostApi.runtime.string.of("cave"));
  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("room-a"),
    entityListMap: { members: [] },
    sector: {
      grid: cave,
      footprint: [[0, 0]],
      at: [0, 0],
      openings: [],
    },
  });
  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("room-b"),
    entityListMap: { members: [] },
    sector: {
      grid: cave,
      footprint: [[0, 0]],
      at: [1, 0],
      openings: [],
    },
  });
};
