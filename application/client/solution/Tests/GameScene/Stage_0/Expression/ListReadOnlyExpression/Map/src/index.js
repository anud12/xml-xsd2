// @ts-check
// Type-level assertion for ListReadOnlyExpression.map, validated by the
// configured lint (solution tsc, checkJs). The callback returns `e.id`
// (a StringReadOnlyExpression), so the mapped list must be
// ListReadOnlyExpression<StringReadOnlyExpression> — not the original
// ListReadOnlyExpression<EntityReadOnlyExpression>.
/** @type {import("module_types/primitives/ListExpression").ListReadOnlyExpression<import("module_types/dataExpression/Entity").EntityReadOnlyExpression>} */
const list = /** @type {any} */ (globalThis);

const mapped = list.map((e) => e.id);

// Fails the lint unless `mapped` is exactly a list of the mapped element type.
/** @type {import("module_types/primitives/ListExpression").ListReadOnlyExpression<import("module_types/primitives/stringExpression").StringReadOnlyExpression>} */
const mappedIsStringList = mapped;

export default mappedIsStringList;
