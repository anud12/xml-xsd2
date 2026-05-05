/** @type {ModuleEntrypoint} */
export default ({string, number, ...hostApi}) => {

  hostApi.setEntity(string.of("entityId"), {
    numberMap: {
      value: number.of(1)
    }
  })
}