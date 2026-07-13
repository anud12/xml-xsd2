// @ts-nocheck
export default (hostApi) => {
  const { number, string } = hostApi;

  hostApi.setEntity(string.of("sword-1"), {
    textMap: { name: string.of("Sword") }
  });

  hostApi.setEntity(string.of("potion-1"), {
    textMap: { name: string.of("Potion") }
  });

  hostApi.setEntity(string.of("shield-1"), {
    textMap: { name: string.of("Shield") }
  });

  hostApi.registerContainer({
    id: "bag-1",
    entities: [
      string.of("sword-1"),
      string.of("potion-1"),
      string.of("shield-1"),
    ],
  });

  hostApi.registerPanel({
    id: "inventory",
    size: {
      height: number.of(400),
      width: number.of(400)
    },
    anchor: {
      x: number.of(0.5),
      y: number.of(0.5)
    },
    background: hostApi.texture.of("texture.exr"),
    content: {
      type: "containerList",
      containerId: string.of("bag-1"),
      align: "center",
      template: (entityId, index) => ({
        id: `item_${index}`,
        size: {
          height: number.of(50),
          width: number.of(50)
        },
        anchor: {
          x: number.of(0),
          y: number.of(index * 0.25)
        },
        content: {
          type: "entityTextValue",
          name: string.of("name"),
          entityId: entityId,
          align: "center"
        }
      })
    }
  });
}
