/** @type {ModuleEntrypoint} */
export default ({string, number, ...hostApi}) => {

  hostApi.setEntity(string.of("entityId"), {
    textMap: {
      value: string.of("1")
    }
  })
}