/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.setEntity(string.of("item-a"), {
    numberMap: { value: number.of(1) },
  });
  hostApi.runtime.setEntity(string.of("item-b"), {
    numberMap: { value: number.of(2) },
  });
  hostApi.runtime.setEntity(string.of("item-c"), {
    numberMap: { value: number.of(3) },
  });
  hostApi.runtime.setContainer(string.of("items-container"), {
    entities: [string.of("item-a"), string.of("item-b"), string.of("item-c")],
  });

  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("texture.png") }],
  });
  hostApi.runtime.registerAnimation(string.of("texture2"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("texture2.png") }],
  });

  hostApi.ui.window("list-panel", {
    width: 300,
    height: 300,
    anchor: "center",
    align: "center",
    background: hostApi.ui.getAnimation(string.of("texture"), { duration: number.of(1) }),
  }, [
    hostApi.ui.container("items", { container: "items-container" },
      (entity) => [
        hostApi.ui.window(entity.id, {
          width: 50,
          height: 50,
          background: hostApi.ui.getAnimation(string.of("texture2"), { duration: number.of(1) }),
        }, [
          hostApi.ui.field(entity.id + ":value", {
            entity: entity.id,
            map: "number",
            name: "value",
            fallback: "0",
          }),
        ]),
      ]),
  ])
}
