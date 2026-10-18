/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  const itemA = hostApi.runtime.setEntity({
    id: string.of("item-a"),
    numberMap: { value: number.of(1) },
  });
  const itemB = hostApi.runtime.setEntity({
    id: string.of("item-b"),
    numberMap: { value: number.of(2) },
  });
  const itemC = hostApi.runtime.setEntity({
    id: string.of("item-c"),
    numberMap: { value: number.of(3) },
  });
  const itemsContainer = hostApi.runtime.setContainer({
    id: string.of("items-container"),
    entities: [itemA, itemB, itemC],
  });

  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("texture.png") }],
    duration: number.of(1),
  });
  const texture2 = hostApi.runtime.setAnimation(string.of("texture2"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("texture2.png") }],
    duration: number.of(1),
  });

  hostApi.ui.window("list-panel", {
    panel: {
      width: 300,
      height: 300,
      anchor: "center",
      align: "center",
      background: texture,
    },
  }, [
    hostApi.ui.entityList("items", { container: itemsContainer },
      (entity) => [
        hostApi.ui.window(entity.id, {
          panel: {
            width: 50,
            height: 50,
            background: texture2,
          },
        }, [
          hostApi.ui.field(entity.id + ":value", {
            entity: entity,
            map: "number",
            name: "value",
            fallback: "0",
          }),
        ]),
      ]),
  ])
}
