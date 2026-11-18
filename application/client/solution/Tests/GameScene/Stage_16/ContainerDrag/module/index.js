/**
 * Stage_16 / ContainerDrag — a window that maps to a container via
 * `entityList`, whose child entities are individually draggable and can be
 * dropped onto another container-mapped window.
 *
 * Two container windows (`container_1`, `container_2`) are declared; each is
 * a `draggable`/`droppable` window that renders its container's entities as
 * child windows (one per entity) through `entityList`. `item-1` and `item-2`
 * start in `container_1`; `container_2` starts empty.
 *
 * The drag type ("land") owns BOTH the shared drop affordance (the `feedback`
 * overlay) and the drop semantics: its `onDrop` moves the entity INTO the
 * drop-off container, identified by `ctx.targetId` (each container window is
 * declared with `id == containerId`). The entity is removed from its source
 * container and re-added to the target (the same re-reference-by-id move used
 * by TestTick).
 */

/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // Drop affordance: a solid-color overlay shown over a container window
  // while a compatible drag hovers it (the highlight is the visual cue that
  // the entity can be released there).
  const highlight = hostApi.runtime.setAnimation(string.of("drop-highlight"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("drop_highlight.png") }],
    duration: number.of(1),
  });

  // Surface texture for the container windows (dark slate).
  const panelBg = hostApi.runtime.setAnimation(string.of("panel-bg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("panel_bg.png") }],
    duration: number.of(1),
  });

  // Distinct surface for each entity's child panel (light blue) so the items
  // are visually distinguishable from the container background they sit on.
  const itemBg = hostApi.runtime.setAnimation(string.of("item-bg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("item_bg.png") }],
    duration: number.of(1),
  });

  // Font for the drop-feedback text label (the same setFont mechanism used by
  // ui.text / ui.field). The label is drawn centered over the feedback overlay
  // and is visible only while the onDrag/onHover feedback is active.
  const feedbackFont = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 22, color: "#FFFFFF" });

  // Two entities that start in container_1; container_2 starts empty.
  const item1 = hostApi.runtime.setEntity({ id: string.of("item-1") });
  const item2 = hostApi.runtime.setEntity({ id: string.of("item-2") });
  const container1 = hostApi.runtime.setContainer({
    id: string.of("container_1"),
    entities: [item1, item2],
  });
  const container2 = hostApi.runtime.setContainer({
    id: string.of("container_2"),
    entities: [],
  });

  // Move effect: removes the entity from `from` and re-adds it to `to`. The
  // entity is re-referenced by id (`setEntity` by id returns a reference to
  // the existing entity) so no captured handle is needed for the add.
  const moveEffect = hostApi.runtime.setEffect({
    name: string.of("move-entity"),
    apply: (context) => {
      const args = globalThis.__actionArgs;
      if (!args) return;
      const entityId = String(args.entityId);
      const from = String(args.from);
      const to = String(args.to);
      if (!entityId || !from || !to || from === to) return;
      context.getMutableContainersBy({
        filters: [(c) => c.id.isContainingExactly(string.of(from))],
      }).forEach((c) => c.removeEntity(string.of(entityId)));
      const entityRef = hostApi.runtime.setEntity({ id: string.of(entityId) });
      context.getMutableContainersBy({
        filters: [(c) => c.id.isContainingExactly(string.of(to))],
      }).forEach((c) => c.addEntity(entityRef));
    },
  });

  const moveAction = hostApi.runtime.setAction({
    name: string.of("move-entity"),
    apply: (ctx) => {
      ctx.emitEffect(moveEffect, {
        entityId: ctx.args.entityId,
        from: ctx.args.from,
        to: ctx.args.to,
      });
    },
  });

  // The drag type owns BOTH the shared drop affordance (feedback) and the drop
  // semantics: `onDrop` moves the entity into the drop-off's container.
  // `ctx.targetId` is the drop-off window's id, and each container window is
  // declared with `id == containerId` — so this single onDrop routes every drop
  // to the right container with no per-target override.
  const LAND = hostApi.ui.dragAndDropChannel({
    name: "land",
    description: "A movable item",
    onDrop: (ctx) => emitMove(ctx),
    feedback: {
      onDrag: { animation: highlight, fill: { width: "stretch", height: "stretch" }, text: { value: string.of("DROP"), font: feedbackFont } },
      onHover: { animation: highlight, fill: { width: "stretch", height: "stretch" }, text: { value: string.of("HERE"), font: feedbackFont } },
    },
  });

  // Drop handler: move the dropped entity into the drop-off's container.
  // `ctx.item.entityId` is the entity id carried by the drag payload;
  // `ctx.item.sourceContainerId` is the container it was dragged from;
  // `ctx.targetId` is the drop-off's (container) id — the destination.
  const emitMove = (ctx) => {
    hostApi.runtime.log(
      "___container drag move " + ctx.item.entityId + " -> " + ctx.targetId + "___"
    );
    ctx.emitAction(moveAction, {
      entityId: ctx.item.entityId,
      from: ctx.item.sourceContainerId,
      to: ctx.targetId,
    });
  };

  // A container window: a droppable panel (accepts the "land" type) that maps
  // to `container` via `entityList`, rendering each member as a draggable child
  // window whose drag payload carries the entity id + source container id. Its
  // window id equals `containerId`, so the type's onDrop routes drops via
  // ctx.targetId.
  const containerWindow = (id, containerId, container, x) =>
    hostApi.ui.window(id, {
      panel: {
        x,
        y: 100,
        width: 220,
        height: 300,
        background: { animation: panelBg, fill: { width: "stretch", height: "stretch" } },
        droppable: LAND.droppable(),
      },
    }, [
      hostApi.ui.entityList(id + "_items", { container }, (entity) => [
        hostApi.ui.window(entity.id, {
          panel: {
            width: 200,
            height: 60,
            background: { animation: itemBg, fill: { width: "stretch", height: "stretch" } },
            draggable: LAND.draggable({ payload: { entityId: entity.id, sourceContainerId: containerId } }),
          },
        }, []),
      ]),
    ]);

  containerWindow("container_1", "container_1", container1, 20);
  containerWindow("container_2", "container_2", container2, 320);
}
