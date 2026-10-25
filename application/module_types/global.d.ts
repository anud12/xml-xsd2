// types/global.d.ts
// Make the ModuleEntrypoint type available as a global type name so JSDoc in JS can use it without a relative import.
import type { ModuleEntrypoint as _ModuleEntrypoint } from "./ModuleEntrypoint";
import {Expression as _Expression} from "./Expression";
import {ConditionReadOnlyExpression as _ConditionReadOnlyExpression} from "./primitives/conditionExpression";
declare global {
  // expose a global type alias named `ModuleEntrypoint`
  type ModuleEntrypoint = _ModuleEntrypoint;
  type Expression = _Expression;
  type ConditionReadOnlyExpression<T extends Expression> = _ConditionReadOnlyExpression<T>;
}

// ensure this file is treated as a module (so `import type` above is allowed)
export {};