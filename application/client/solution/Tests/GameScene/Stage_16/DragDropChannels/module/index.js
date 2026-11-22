/**
 * Stage_16 / DragDropChannels — verifies that INDEPENDENT drag channels are
 * isolated from one another.
 *
 * Two drag channels are declared: "channel-a" and "channel-b". Each channel
 * has a draggable source panel and a droppable target panel that accepts ONLY
 * that channel:
 *
 *   source-a  (draggable, emits channel-a)  ──►  target-a  (accepts channel-a)  ──►  bin-a
 *   source-b  (draggable, emits channel-b)  ──►  target-b  (accepts channel-b)  ──►  bin-b
 *
 * The point of the test is the "available zones" feedback: when a channel-a
 * drag is in flight, ONLY target-a (which accepts channel-a) shows its ambient
 * feedback indicator — target-b (which accepts only channel-b) must stay dark
 * (feedback state None), and vice versa for a channel-b drag. A drop on the
 * matching target moves the entity into that channel's bin (the data path).
 */

/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  // Two DISTINCT feedback colors so the two drop states are visually evident:
  //   drag-zone  (cool blue)    — the ambient "available drop zone" (onDrag)
  //   hover-zone (warm orange)  — the "release to drop" highlight (onHover)
  const dragZone = hostApi.runtime.setAnimation(string.of("drag-zone"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("drag_zone.png") }],
    duration: number.of(1),
  });
  const hoverZone = hostApi.runtime.setAnimation(string.of("hover-zone"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("hover_zone.png") }],
    duration: number.of(1),
  });
  // Visible surface for the four panels.
  const panelBg = hostApi.runtime.setAnimation(string.of("panel-bg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("panel_bg.png") }],
    duration: number.of(1),
  });
  // A shared TextureFill. Annotated (rather than inferred) so the string
  // literals stay the narrow "stretch" type in this checked .js module.
  /** @type {{ width: "stretch" | "tile" | "fit", height: "stretch" | "tile" | "fit" }} */
  const fill = { width: "stretch", height: "stretch" };
  // Font for the drop-feedback text label — the same setFont mechanism used by
  // ui.text / ui.field. The label is drawn centered over the feedback overlay
  // and is visible ONLY while the onDrag/onHover feedback is active.
  const feedbackFont = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 22, color: "#FFFFFF" });

  // Entities + containers. item-a starts in tray-a, item-b in tray-b; the bins
  // start empty. A drop moves the entity from its tray into its channel's bin.
  const itemA = hostApi.runtime.setEntity({ id: string.of("item-a") });
  const itemB = hostApi.runtime.setEntity({ id: string.of("item-b") });
  hostApi.runtime.setEntity({
    id: string.of("tray-a"),
    entityListMap: { members: [itemA] },
  });
  hostApi.runtime.setEntity({
    id: string.of("tray-b"),
    entityListMap: { members: [itemB] },
  });
  hostApi.runtime.setEntity({
    id: string.of("bin-a"),
    entityListMap: { members: [] },
  });
  hostApi.runtime.setEntity({
    id: string.of("bin-b"),
    entityListMap: { members: [] },
  });

  // Shared move effect: remove the entity from `from`, re-add it to `to`. The
  // entity is re-referenced by id so no captured handle is needed for the add.
  const moveEffect = hostApi.runtime.setEffect({
    name: string.of("move-entity"),
    apply: (context) => {
      const args = globalThis.__actionArgs;
      if (!args) return;
      const entityId = String(args.entityId);
      const from = String(args.from);
      const to = String(args.to);
      if (!entityId || !from || !to || from === to) return;
      context.getMutableEntityBy({
        filters: [(c) => c.id.isContainingExactly(string.of(from))],
      }).forEach((c) => c.removeMember("members", string.of(entityId)));
      const entity = hostApi.runtime.setEntity({ id: string.of(entityId) });
      context.getMutableEntityBy({
        filters: [(c) => c.id.isContainingExactly(string.of(to))],
      }).forEach((c) => c.addMember("members", entity));
    },
  });

  // Shared move action: forwards its args (entityId, from, to) to the move
  // effect. The dropped entity's SOURCE is `from` and the destination is `to`;
  // each channel's onDrop supplies the right `to` (bin-a vs bin-b). The args
  // travel through the action-args bridge (persisted on the sim context as
  // __actionArgs), which the effect reads — so `from`/`to` MUST be passed to
  // the action, not just to the effect.
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

  // Two INDEPENDENT drag channels. Each owns its onDrop (move into its bin) and
  // its feedback affordance (ambient `onDrag` + `onHover` highlight).
  const channelA = hostApi.ui.dragAndDropChannel({
    name: "channel-a",
    description: "Channel A: drops item-a into bin-a",
    onDrop: (ctx) => {
      hostApi.runtime.log("___channel-a onDrop " + ctx.item.entityId + "___");
      ctx.emitAction(moveAction, {
        entityId: ctx.item.entityId,
        from: ctx.item.sourceContainerId,
        to: "bin-a",
      });
    },
    feedback: {
      onDrag: { animation: dragZone, fill, text: { value: string.of("DROP"), font: feedbackFont } },
      onHover: { animation: hoverZone, fill, text: { value: string.of("HERE"), font: feedbackFont } },
    },
  });
  const channelB = hostApi.ui.dragAndDropChannel({
    name: "channel-b",
    description: "Channel B: drops item-b into bin-b",
    onDrop: (ctx) => {
      hostApi.runtime.log("___channel-b onDrop " + ctx.item.entityId + "___");
      ctx.emitAction(moveAction, {
        entityId: ctx.item.entityId,
        from: ctx.item.sourceContainerId,
        to: "bin-b",
      });
    },
    feedback: {
      onDrag: { animation: dragZone, fill, text: { value: string.of("DROP"), font: feedbackFont } },
      onHover: { animation: hoverZone, fill, text: { value: string.of("HERE"), font: feedbackFont } },
    },
  });

  // Four panels (two sources, two targets), laid out in a 2x2 grid below the
  // coreUI button row. target-a accepts ONLY channel-a; target-b ONLY channel-b.
  hostApi.ui.window("source-a", {
    panel: {
      x: 20, y: 100, width: 200, height: 80,
      background: { animation: panelBg, fill },
      draggable: channelA.draggable({ payload: { entityId: "item-a", sourceContainerId: "tray-a" } }),
    },
  }, []);
  hostApi.ui.window("source-b", {
    panel: {
      x: 20, y: 250, width: 200, height: 80,
      background: { animation: panelBg, fill },
      draggable: channelB.draggable({ payload: { entityId: "item-b", sourceContainerId: "tray-b" } }),
    },
  }, []);
  hostApi.ui.window("target-a", {
    panel: {
      x: 280, y: 100, width: 200, height: 80,
      background: { animation: panelBg, fill },
      droppable: channelA.droppable(),
    },
  }, []);
  hostApi.ui.window("target-b", {
    panel: {
      x: 280, y: 250, width: 200, height: 80,
      background: { animation: panelBg, fill },
      droppable: channelB.droppable(),
    },
  }, []);
};
