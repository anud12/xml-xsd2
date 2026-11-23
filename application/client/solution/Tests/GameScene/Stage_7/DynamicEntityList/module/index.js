/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  const sword = hostApi.runtime.setEntity({
    id: string.of("sword-1"),
    numberMap: { attack: number.of(25) },
    stringMap: { name: string.of("Sword") },
  });

  const shield = hostApi.runtime.setEntity({
    id: string.of("shield-1"),
    numberMap: { defense: number.of(15) },
    stringMap: { name: string.of("Shield") },
  });

  const inventory = hostApi.runtime.setEntity({
    id: string.of("inventory"),
    entityListMap: { members: [] },
  });

  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 14, color: "#FFFFFF" });

  hostApi.ui.window("inv-list", {
    panel: {
      x: 10,
      y: 10,
      width: 200,
      height: 300,
      title: "Inventory",
    },
  }, [
    hostApi.ui.entityList("inv-items", { group: inventory, list: "members" },
      (entity) => [
        hostApi.ui.window(entity.id, {
          panel: {
            width: 180,
            height: 40,
            layout: "column",
          },
        }, [
          hostApi.ui.field(entity.id + ":name", {
            entity: entity.entity,
            map: "text",
            name: "name",
            fallback: "Unknown",
          }, font),
        ]),
      ]),
  ]);

  const addToInventoryEffect = hostApi.runtime.setEffect({
    name: string.of("add-to-inventory"),
    apply: (context) => {
      context.getMutableEntityBy({
        filters: [(c) => c.id.isContainingExactly(string.of("inventory"))],
      }).forEach((container) => {
        container.addMember("members", sword);
      });
    },
  });

  const addShieldToInventoryEffect = hostApi.runtime.setEffect({
    name: string.of("add-shield-to-inventory"),
    apply: (context) => {
      context.getMutableEntityBy({
        filters: [(c) => c.id.isContainingExactly(string.of("inventory"))],
      }).forEach((container) => {
        container.addMember("members", shield);
      });
    },
  });

  hostApi.runtime.setAction({
    name: string.of("add-sword"),
    apply: (context) => {
      context.emitEffect(addToInventoryEffect, {});
    },
  });

  hostApi.runtime.setAction({
    name: string.of("add-shield"),
    apply: (context) => {
      context.emitEffect(addShieldToInventoryEffect, {});
    },
  });
}
