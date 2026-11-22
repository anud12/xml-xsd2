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
      openings: [
        { cell: [0, 0], side: "E", start: hostApi.runtime.number.of(0), length: hostApi.runtime.number.of(10) },
      ],
    },
  });
  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("room-b"),
    entityListMap: { members: [] },
    sector: {
      grid: cave,
      footprint: [[0, 0]],
      at: [1, 0],
      openings: [
        { cell: [0, 0], side: "W", start: hostApi.runtime.number.of(0), length: hostApi.runtime.number.of(3) },
      ],
    },
  });
};
