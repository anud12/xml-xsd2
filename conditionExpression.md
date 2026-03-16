# Condition expression — Minimal API

This document specifies a minimal ConditionExpression API that exposes exactly four functions and no other boolean constructors or operators.

---

## Problem Understanding
Provide a tiny, well-defined boolean-expression surface usable by host scripts and runtime rule evaluation. The surface MUST contain only:
- \`true()\` => ConditionExpression
- \`false()\` => ConditionExpression
- ConditionExpression.and(other: ConditionExpression) => ConditionExpression
- ConditionExpression.or(other: ConditionExpression) => ConditionExpression

Expressions are immutable and evaluation is a runtime concern.

---

## Missing Information
- Should \`true()\`/\`false()\` return singletons or new node instances each call?  
- Should the model be serializable (for tooling/inspection)?  
- Should an explicit \`evaluate()\` helper be exposed, or must evaluation remain strictly a runtime/internal API?

---

## Proposed Architecture
- Shape: a small immutable AST with four node kinds:
  - TrueLiteral
  - FalseLiteral
  - AndNode(left, right)
  - OrNode(left, right)
- Construction: factories \`true()\` and \`false()\` create leaf nodes. \`and()\`/\`or()\` produce binary nodes linking children.
- Evaluation: performed by the host runtime when the expression is applied (for example when used as a guard). The ConditionExpression type itself does not expose evaluation; it only describes the tree.

Host API (TypeScript)
\`\`\`ts
export type ConditionExpression = {
  /** Short-circuiting combinators. Immutable. */
  and: (other: ConditionExpression) => ConditionExpression;
  or:  (other: ConditionExpression) => ConditionExpression;
};

export type ConditionExpressionApi = {
  /** Factory functions (exact names required by spec) */
  'true':  () => ConditionExpression;
  'false': () => ConditionExpression;

  /** Marker for HostApi surfaces */
  type: ConditionExpressionType;
};
\`\`\`

---

## Data Flow
1. Host/client code constructs expression trees via \`hostApi.boolean.true()\` / \`hostApi.boolean.false()\` and combinators.
2. Expression trees are handed to runtime subsystems (guards, rule activation).
3. Runtime evaluates the tree left-to-right using short-circuit rules:
   - \`and\`: evaluate left; if false → result false; else evaluate right.
   - \`or\`:  evaluate left; if true  → result true; else evaluate right.
4. Runtime returns a host boolean result to the caller.

---

## Failure Modes & Edge Cases
- Expressiveness: no \`not\`, \`ref\`, \`oneOf\`, \`xor\`, or \`implies\`. Some logic cannot be expressed directly.
- Deep nesting: naive recursive evaluators may overflow the stack; prefer iterative or bounded recursion.
- Side effects: any side effects in evaluation (if introduced elsewhere) will be affected by short-circuiting semantics.
- Serialization/inspection: absent unless explicitly implemented.
- API name collision: \`true\`/\`false\` are literal words — ensure host bindings or type systems accept these property names (use quoted keys if necessary).

Mitigations:
- Provide companion utility library for \`not\`, \`ref\`, \`oneOf\` if needed (implemented outside core API).
- Implement evaluation iteratively or with an explicit evaluation stack.
- Document short-circuiting clearly so implementers avoid hidden side effects in referenced evaluation points.

---

## Tradeoffs
- + Extremely small, easy-to-validate API surface; simpler tooling and indexing.
- + Strong immutability and predictable short-circuit semantics.
- − Reduced expressiveness; callers may need additional helper layers.
- − Some integration choices (serialization, singletons) left open, requiring policy decisions.

---

## Open Questions
- Singleton vs. fresh-node factories for \`true()\`/\`false()\`? (memory vs identity semantics)
- Should evaluation be exposed (e.g., \`ConditionExpression.evaluate(world): boolean\`) for unit testing or debugging?
- Should condition expressions be serializable to/from a stable representation for tooling?
- Naming: allow \`hostApi.boolean.true()\` access via dot syntax in all target host languages? (if not, use bracket access or different identifiers)

(If you want, pick one option per question — or choose "Other" and describe a preferred alternative.)

---

## Next Iteration
- Decide on factory identity/seeding (singleton vs new instance).
- If needed, design a small companion helper module providing \`not()\`, \`ref()\`, and \`oneOf()\` implemented outside the core four-function surface.
- Draft evaluation guidelines (iterative evaluator, maximum depth, logging hooks).
- Add serialization format if tooling requires it.

---

Architecture Review Result: Not Implementation Ready — final readiness awaits decisions on factory identity and whether evaluation/serialization helpers should be part of the public surface. Quack: pick the two small choices above and this becomes rock-solid.
