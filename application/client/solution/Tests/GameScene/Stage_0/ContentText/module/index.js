/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const entity = hostApi.runtime.setEntity({
    id: string.of("entity_id"),
    textMap: {
      "textKey": string.of("textValue")
    },
    numberMap: {
      "numberKey": number.of(42)
    },
  });

  hostApi.ui.panel("text-host", {
    width: 100,
    height: 100,
  }, [
    hostApi.ui.text("text-content", "hello"),
  ])

  hostApi.ui.panel("field-host", {
    width: 100,
    height: 100,
    x: 150,
  }, [
    hostApi.ui.field("field-text", { entity: entity, map: "text", name: "textKey", fallback: "fallback" }),
  ])

  hostApi.ui.panel("number-host", {
    width: 100,
    height: 100,
    x: 300,
  }, [
    hostApi.ui.field("number-text", { entity: entity, map: "number", name: "numberKey", fallback: "?" }),
  ])
}
