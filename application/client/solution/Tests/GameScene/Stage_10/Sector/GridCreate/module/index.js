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
        {
          cell: [0, 0],
          side: "N",
          start: hostApi.runtime.number.of(13),
          length: hostApi.runtime.number.of(4),
        },
      ],
    },
  });
};
