/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });
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


  hostApi.ui.window("text-host", {
    panel: {
      width: 100,
      height: 100,
    },
  }, [
    hostApi.ui.text("text-content", "hello", font),
  ])

  hostApi.ui.window("field-host", {
    panel: {
      width: 100,
      height: 100,
      x: 150,
    },
  }, [
    hostApi.ui.field("field-text", { entity: entity, map: "text", name: "textKey", fallback: "fallback" }, font),
  ])

  hostApi.ui.window("number-host", {
    panel: {
      width: 100,
      height: 100,
      x: 300,
    },
  }, [
    hostApi.ui.field("number-text", { entity: entity, map: "number", name: "numberKey", fallback: "?" }, font),
  ])
}