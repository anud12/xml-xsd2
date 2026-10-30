// @ts-check
// Type-level assertion for ContainerReadOnlyExpression.entities, validated by
// the configured lint (solution tsc, checkJs). `entities` must be
// ArrayReadOnlyExpression<EntityReadOnlyExpression> — not a plain Array.
/** @type {import("module_types/dataExpression/Contaier").ContainerReadOnlyExpression} */
const container = /** @type {any} */ (globalThis);

// Fails the lint unless `.entities` is exactly ArrayReadOnlyExpression<Entity>.
/** @type {import("module_types/primitives/arrayExpression").ArrayReadOnlyExpression<import("module_types/dataExpression/Entity").EntityReadOnlyExpression>} */
const entities = container.entities;

export default entities;
