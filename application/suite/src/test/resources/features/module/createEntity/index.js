/**
 */
/** @type {ModuleEntrypoint} */
export default ({string, entity, textMap, ...hostApi}) => {
  hostApi.emitEvent("empty event", {})
  hostApi.registerEvent({
    name: "empty event",
    prepare: (context, input) => {
      return {}
    },
    apply: (context, output) => {
      context
        .createEntity(entity.create()
          .withTextMap(textMap.create()
            .put("firstName", string.of("Dave"))
          )
        )
        .createEntity(entity.create()
          .withTextMap(textMap.create()
            .put("firstName", string.of("John"))
          )
        )
    }
  })

}