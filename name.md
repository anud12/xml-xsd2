# Name — Concepts

This document describes the core "Name" concept used by the XML world_step model.

## Summary

The "name" subsystem is a rule-driven name-generation engine. NameRules (world_step.rule_group.name_rule) contain entries (entry id="...") composed of one or more name_token elements. The runtime indexes entries by id and resolves names by concatenating token prefixes, resolving references to other name rules, and selecting alternatives from one_of groups.

## Implementation mapping

- world_step.rule_group.name_rule -> ro.anud.xml_xsd.implementation.model.WorldStep.RuleGroup.NameRule.NameRule
- entry -> ro.anud.xml_xsd.implementation.model.WorldStep.RuleGroup.NameRule.Entry.Entry (attribute: id)
- name_token -> ro.anud.xml_xsd.implementation.model.Type_nameToken.NameToken.NameToken
  - prefix (required) -> NameToken.getPrefix()
  - ref (optional) -> Type_nameToken.NameToken._ref._ref with attribute name_rule_ref
  - one_of (optional) -> Type_nameToken.NameToken.OneOf.OneOf containing list of name_token

## Indexing

1. WorldStepInstance.index() triggers name.index() -> NameInstance.index() -> Repository.index()
2. Repository.index() iterates worldStep.streamRuleGroup() -> RuleGroup.streamNameRule() -> NameRule.streamEntry() and populates a HashMap id -> Entry (service.name.Repository.stringNameRuleHashMap).
3. Repository.getNameTokenById(String id) returns Optional<Entry>.

## Resolution algorithm (order & behavior)

- API: NameInstance.calculateNameFromRefString(String nameRuleRef) -> CalculateName.calculateNameFromRefString(worldStepInstance, ref)
- CalculateName:
  - Looks up Entry by id via repository.getNameTokenById(ref)
  - For each NameToken in Entry.streamNameToken():
    1. Start with prefix = nameToken.getPrefix() (prefix is required)
    2. Append referenced name (if present): nameToken.get_ref().flatMap(r -> calculateNameFromRefString(worldStepInstance, r.getNameRuleRef())).orElse("") — missing or unknown refs yield empty string (fail-soft inside token)
    3. Append selected one_of child (if present): group.getNameToken() -> worldStepInstance.randomFrom(list) -> recursively evaluate that child; append its result
  - Tokens are concatenated in order and the final string is returned wrapped in Optional.of(result).

## Determinism and randomness

- WorldStepInstance.random() produces a deterministic float using WorldMetadata.RandomizationTable and an internal counter; selection is reproducible per WorldStepInstance.
- Selection via randomFrom(list) currently computes int randomIndex = (int) Math.floor(this.random() * (list.size() - 1)); this excludes the last element when list.size() > 1. Known issue: the last element is never selected. Recommended fix: use Math.floor(random() * list.size()) or a standard RNG seeded from WorldStepInstance for reproducible, uniform selection.

## Validation

- NameRuleRefValidator.getAllowedValues(WorldStep) returns Stream<String> of allowed name_rule_ref values (all Entry.getId values).

## Examples (from spec tests)

- one_of: outer name_token prefix="prefix", inner one_of contains a name_token prefix="first one_of" -> resolved name: "prefixfirst one_of".

## Edge cases & notes

- prefix is required (NameToken.deserialize enforces getAttributeRequired("prefix")).
- Empty one_of or missing/empty name_token lists produce empty appended pieces.
- Top-level NameInstance.calculateNameFromRefString(Optional<String>) returns Optional.empty for empty input; CalculateName returns Optional.of(result) even when result is empty string.
- Missing name_rule references inside a token are treated as empty strings when resolving children; consider stricter handling where desired.

## Recommendations

- Fix randomFrom index computation to include all list elements.
- Add explicit tests asserting uniform selection and inclusion of all alternatives in one_of groups.
- Consider optional warnings or validation failures for unresolved references depending on desired strictness.

## Key classes / methods (reference)

- ro.anud.xml_xsd.implementation.service.WorldStepInstance#index()
- ro.anud.xml_xsd.implementation.service.name.NameInstance#index()
- ro.anud.xml_xsd.implementation.service.name.Repository#index(), getNameTokenById()
- ro.anud.xml_xsd.implementation.service.name.CalculateName#calculateNameFromRefString
- ro.anud.xml_xsd.implementation.model.Type_nameToken.NameToken.NameToken
- ro.anud.xml_xsd.implementation.model.Type_nameToken.NameToken._ref._ref
- ro.anud.xml_xsd.implementation.model.Type_nameToken.NameToken.OneOf.OneOf
- ro.anud.xml_xsd.implementation.model.WorldStep.RuleGroup.NameRule.NameRule
- ro.anud.xml_xsd.implementation.model.WorldStep.RuleGroup.NameRule.Entry.Entry
- ro.anud.xml_xsd.implementation.validator.attributeValidator.NameRuleRefValidator
