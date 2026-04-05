/**
 * Test module: No-input action registration and entity creation
 * 
 * This module tests the registerAction() API for no-input actions.
 * When a no-input action is executed, it emits an event that triggers
 * an effect to create a new entity.
 * 
 * @type {ModuleEntrypoint}
 */
export default function (hostApi) {

  // Register an effect that creates a new entity
  hostApi.registerEffect({
    name: 'create_entity_effect',
    prepare: (eventContext) => {
      return {
        name: hostApi.string.of("summoned"),
      };
    },
    apply: (eventContext,preparedOutput) => {
      eventContext.createEntity(hostApi.entity.create()
        .withTextMap(hostApi.textMap.create().put("name", preparedOutput.name)))
    }
  });

  // Register a no-input action that emits an event
  hostApi.registerAction({
    name: 'summon',
    apply: (actionContext) => {
      actionContext.emitEvent('create_entity_effect', {});
    }
  });
}
