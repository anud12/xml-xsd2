/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.setEntity(string.of("item-a"), {
    numberMap: {
      value: number.of(1),
    },
  });

  hostApi.runtime.setEntity(string.of("item-b"),{
    numberMap: {
      value: number.of(2),
    },
  });

  hostApi.runtime.setEntity(string.of("item-c"), {
    numberMap: {
      value: number.of(3),
    },
  });

  hostApi.runtime.setContainer(string.of("items-container"),{
    entities: [
      string.of("item-a"),
      string.of("item-b"),
      string.of("item-c"),
    ],
  });

  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture.png"), {
    frames: [
      { sprite: hostApi.ui.texture.getSpritePNG("texture.png"), gtu: hostApi.runtime.number.of(10) },
    ],
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture2.png"), {
    frames: [
      { sprite: hostApi.ui.texture.getSpritePNG("texture2.png"), gtu: hostApi.runtime.number.of(10) },
    ],
  });
  hostApi.ui.registerPanel({
    id: "list-panel",
    size: {
      height: number.of(300),
      width: number.of(300)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5),
    },
    background: hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture.png")),
    content: {
      type: "containerListView",
      containerId: string.of("items-container"),
      vertical: true,
      align: "center",
      template: (entity, index) => ({
        id:`entity_${index}`,
        size: {
          height: number.of(50),
          width: number.of(100)
        },
        background:  hostApi.ui.texture.getAnimation(hostApi.runtime.string.of("texture2.png")),
        content: {
          entityId: entity.getId(),
          name: string.of("value"),
          type: "entityNumberValue",
          align: "center",
        }
      })
    }
  });
}
