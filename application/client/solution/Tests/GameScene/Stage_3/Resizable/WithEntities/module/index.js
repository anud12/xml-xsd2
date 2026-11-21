/**
 * A panel that is BOTH resizable and a container of child entities. The
 * entities are rendered as child windows through `entityList`, which is a
 * division (container view). The division must pass mouse input through so the
 * panel's corner-drag resize hit zone stays reachable: without that, the
 * entityList would swallow the corner press and the panel could never be
 * resized.
 */
/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("texture.png") }],
    duration: number.of(1),
  });
  const itemTexture = hostApi.runtime.setAnimation(string.of("item"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("item.png") }],
    duration: number.of(1),
  });

  // Two child entities that the panel renders through its entityList.
  const entA = hostApi.runtime.setEntity({ id: string.of("ent-a") });
  const entB = hostApi.runtime.setEntity({ id: string.of("ent-b") });
  const entContainer = hostApi.runtime.setContainer({
    id: string.of("ent-container"),
    entities: [entA, entB],
  });

  hostApi.ui.window("resizable-entities", {
    panel: {
      width: 200,
      height: 200,
      x: 350,
      y: 100,
      resizable: { horizontal: true, vertical: true },
      background: { animation: texture, fill: { width: "stretch", height: "stretch" } },
    },
  }, [
    hostApi.ui.entityList("ents", { container: entContainer }, (entity) => [
      hostApi.ui.window(entity.id, {
        panel: {
          width: 60,
          height: 20,
          background: { animation: itemTexture, fill: { width: "stretch", height: "stretch" } },
        },
      }, []),
    ]),
  ]);
}
