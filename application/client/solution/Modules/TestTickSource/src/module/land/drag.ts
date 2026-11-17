import {HostApi} from "module_types/HostApi";
import {DragTypeReference} from "module_types/ui/dragdrop";
import {getInventoryModule} from "../inventory/inventory";
import {memoize} from "../../util/memoize";

/**
 * The "land" drag type: the single home for the drop semantics of dragging a
 * land parcel onto the inventory. When such a drag is dropped, the inventory's
 * `buy` action fires with the dragged land's id — the exact same action the
 * in-row "Buy" button emits, so both paths share one purchase effect (cost
 * deduction + container move).
 *
 * Registered once (memoized here, and idempotent by name in the host) and
 * shared by the land list (drag source) and the inventory window (drop target).
 * `onDrop` reads the dragged id off `ctx.item.entityId`; `draggable` tags each
 * land with `{ entityId, sourceContainerId }`.
 */
export const getLandDragType = memoize((hostApi: HostApi): DragTypeReference => {
  const inventory = getInventoryModule(hostApi);
  // Drop-target feedback: the hover overlay shown over the inventory while a
  // "land" drag hovers it. Now REQUIRED on the drag type — a solid fill
  // stretched over the drop area as the drop affordance.
  const feedback = hostApi.runtime.setAnimation(
    hostApi.runtime.string.of("land-drop-feedback"),
    {
      frames: [{ sprite: hostApi.ui.getSpritePNG("assets/drop_highlight.png") }],
      duration: hostApi.runtime.number.of(1),
    },
  );
  return hostApi.ui.dragAndDrop({
    name: "land",
    description: "A land parcel; drop it on the inventory to buy it",
    onDrop: (ctx) => {
      ctx.emitAction(inventory.buy, { entityId: ctx.item.entityId });
    },
    feedback: {
      animation: feedback,
      fill: { width: "stretch", height: "stretch" },
    },
  });
});
