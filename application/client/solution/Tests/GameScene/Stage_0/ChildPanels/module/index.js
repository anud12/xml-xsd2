/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.window("parent", {
    panel: {
      width: 100,
      height: 100,
    },
  }, [
    hostApi.ui.window("child", {
      panel: {
        width: 20,
        height: 20,
        x: 10,
        y: 10,
      },
    }, []),
    hostApi.ui.window("child_2", {
      panel: {
        width: 20,
        height: 20,
        x: 30,
        y: 30,
      },
    }, []),
  ])
}
