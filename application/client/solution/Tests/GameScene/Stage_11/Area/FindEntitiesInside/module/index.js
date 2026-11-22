/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // A large "room" that declares a named 100x100 local area ("floor"). Its
  // container world position is (10, 10), so its world polygon spans
  // (10,10)..(110,110).
  const room = hostApi.runtime.setEntity({
    id: string.of("room"),
    numberMap: { column: number.of(10), row: number.of(10) },
    stringMap: { xKey: "column", yKey: "row" },
    polygons: { floor: [[0, 0], [100, 0], [100, 100], [0, 100]] },
  });

  // A "crate" with its own named 20x20 area, world (40,40): fully inside the room.
  const crate = hostApi.runtime.setEntity({
    id: string.of("crate"),
    numberMap: { column: number.of(40), row: number.of(40) },
    stringMap: { xKey: "column", yKey: "row" },
    polygons: { footprint: [[0, 0], [20, 0], [20, 20], [0, 20]] },
  });

  // Point members (no area): a point at (30,40) is inside, (500,500) is not.
  const npcIn = hostApi.runtime.setEntity({
    id: string.of("npc-in"),
    numberMap: { column: number.of(30), row: number.of(40) },
    stringMap: { xKey: "column", yKey: "row" },
  });
  const npcOut = hostApi.runtime.setEntity({
    id: string.of("npc-out"),
    numberMap: { column: number.of(500), row: number.of(500) },
    stringMap: { xKey: "column", yKey: "row" },
  });

  hostApi.runtime.setEntity({
    id: string.of("cave"),
    entityListMap: { members: [
      room,
      crate,
      npcIn,
      npcOut,
    ] },
    numberMap: { sizeX: number.of(10), sizeY: number.of(5) },
    stringMap: { outOfBoundsX: "clamp", outOfBoundsY: "clamp" },
  });

  // The room's declared area contains the crate (overlap) and npc-in (point),
  // but not npc-out. The query is read from the Rust-side precomputed map.
  hostApi.runtime.setAction({
    name: string.of("query-room"),
    apply: (ctx) => {
      const entity = ctx.getEntityBy({
        filters: [(e) => e.id.isContainingExactly(hostApi.runtime.string.of("room"))],
      });
      entity.forEach((e) => {
        let out = [];
        e.getEntitiesInsideArea(hostApi.runtime.string.of("floor")).forEach((member) => {
          out.push(String(member.id));
        });
        hostApi.runtime.log("___area-inside:" + out.join(",") + "___");
      });
    },
  });
};
