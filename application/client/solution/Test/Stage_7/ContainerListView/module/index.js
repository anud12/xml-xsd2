// @ts-nocheck
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.registerEntity({
    id: "item-a",
    numberMap: {
      value: number.of(1),
    },
  });

  hostApi.runtime.registerEntity({
    id: "item-b",
    numberMap: {
      value: number.of(2),
    },
  });

  hostApi.runtime.registerEntity({
    id: "item-c",
    numberMap: {
      value: number.of(3),
    },
  });

  hostApi.runtime.registerContainer({
    id: "items-container",
    entities: [
      string.of("item-a"),
      string.of("item-b"),
      string.of("item-c"),
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
    background: hostApi.ui.texture.of("texture.png"),
    content: {
      type: "containerListView",
      containerId: string.of("items-container"),
      vertical: true,
        template: (entity, index) => ({
          entityId: entity.id,
          name: string.of("value"),
          type: "entityNumberValue",
          align: "center",
        })
    }
  });
}
