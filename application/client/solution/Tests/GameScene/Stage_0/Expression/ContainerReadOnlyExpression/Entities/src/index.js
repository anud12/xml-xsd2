// @ts-check
// Verify that named membership reads yield a list expression, not an array.
/** @type {import("module_types/dataExpression/Entity").EntityReadOnlyExpression} */
const group = /** @type {any} */ (globalThis);
/** @type {import("module_types/primitives/ListExpression").ListReadOnlyExpression<import("module_types/dataExpression/Entity").EntityReadOnlyExpression>} */
const members = group.members("inventory");
export default members;
