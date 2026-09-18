/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.world.sectorGrid(hostApi.runtime.string.of("cave"));
  hostApi.runtime.setContainer(hostApi.runtime.string.of("room-a"), {
    entities: [],
    sector: {
      grid: hostApi.runtime.string.of("cave"),
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
