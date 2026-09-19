/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // A large "room" that declares a 100x100 local area. Its container world
  // position is (10, 10), so its world polygon spans (10,10)..(110,110).
  hostApi.runtime.setEntity(string.of("room"), {
    numberMap: { column: number.of(10), row: number.of(10) },
    area: {
      polygon: [[0, 0], [100, 0], [100, 100], [0, 100]],
    },
  });

  // A "crate" with its own 20x20 area, world (40,40): fully inside the room.
  hostApi.runtime.setEntity(string.of("crate"), {
    numberMap: { column: number.of(40), row: number.of(40) },
    area: {
      polygon: [[0, 0], [20, 0], [20, 20], [0, 20]],
    },
  });

  // Point members (no area): a point at (30,40) is inside, (500,500) is not.
  hostApi.runtime.setEntity(string.of("npc-in"), {
    numberMap: { column: number.of(30), row: number.of(40) },
  });
  hostApi.runtime.setEntity(string.of("npc-out"), {
    numberMap: { column: number.of(500), row: number.of(500) },
  });

  hostApi.runtime.setContainer(string.of("cave"), {
    entities: [
      string.of("room"),
      string.of("crate"),
      string.of("npc-in"),
      string.of("npc-out"),
    ],
    getX: (entity) => entity.number_map.get("column").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("row").orElse(number.of(0)),
    sizeX: { value: number.of(10), outOfBounds: "clamp" },
    sizeY: { value: number.of(5), outOfBounds: "clamp" },
  });

  // The room's declared area contains the crate (overlap) and npc-in (point),
  // but not npc-out. The query is read from the Rust-side precomputed map.
  hostApi.runtime.registerAction({
    name: string.of("query-room"),
    apply: (ctx) => {
      const entity = ctx.getEntityBy(
        hostApi.runtime.string.of("room"),
      );
      entity.ifPresent((e) => {
        let out = [];
        e.getEntitiesInsideArea().forEach((member) => {
          out.push(member.getId());
        });
        hostApi.runtime.log("___area-inside:" + out.join(",") + "___");
      });
    },
  });
};
