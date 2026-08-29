/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  hostApi.runtime.setEntity(string.of("entity_id"), {
    textMap: {
      "textKey": string.of("textValue")
    },
    numberMap: {
      "numberKey": number.of(42)
    },
  });

  hostApi.ui.window("text-host", {
    width: 100,
    height: 100,
  }, [
    hostApi.ui.text("text-content", "hello"),
  ])

  hostApi.ui.window("field-host", {
    width: 100,
    height: 100,
    x: 150,
  }, [
    hostApi.ui.field("field-text", { entity: "entity_id", map: "text", name: "textKey", fallback: "fallback" }),
  ])

  hostApi.ui.window("number-host", {
    width: 100,
    height: 100,
    x: 300,
  }, [
    hostApi.ui.field("number-text", { entity: "entity_id", map: "number", name: "numberKey", fallback: "?" }),
  ])
}
