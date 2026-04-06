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
        name: hostApi.string.of("entityName"),
      };
    },
    apply: (eventContext,preparedOutput) => {
      eventContext.createEntity(hostApi.entity.create()
        .withTextMap(hostApi.textMap.create().put("entityName", preparedOutput.name)))
    }
  });

  // Register a no-input action that emits an event
  hostApi.registerAction({
    name: 'create_entity_action',
    apply: (actionContext) => {
      actionContext.emitEvent('create_entity_effect', {});
    }
  });

  hostApi.registerEffect({
    name:"append_name_effect",
    input: {
      entity: {
        type: hostApi.entity.type,
        description:"Entity to modify append name"
      }
    },
    output: {
      entity: {
        type: hostApi.entity.type,
        description:"Entity to modify append name"
      }
    },
    prepare: (context, input) => {
      return input
    },
    apply: (context, output) => {
      const entity = context.getEntityBy(hostApi.entity.filter.create()
        .hasTextValue(hostApi.string.of("name"), value => value.isContainingExactly(hostApi.string.of("summoned")))
      ).randomElement();

      entity.ifPresent(entity => entity
        .getText(hostApi.string.of("name"))
        .ifPresent(name => name.concat(hostApi.string.of("_suffix")))
      )

    }
  });

  hostApi.registerAction({
    name: "append_name_action",
    apply: context => {
      context.emitEvent("append_name_effect", {});
    }
  })
}
