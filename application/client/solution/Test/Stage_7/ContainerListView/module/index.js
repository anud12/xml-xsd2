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

  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture2"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture2.png") },
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
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(1) }),
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
        background:  hostApi.ui.getAnimation(hostApi.runtime.string.of("texture2"), { duration: hostApi.runtime.number.of(1) }),
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
