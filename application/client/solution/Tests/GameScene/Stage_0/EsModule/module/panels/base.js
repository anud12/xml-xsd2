export function buildBasePanel(hostApi) {
    hostApi.ui.window("base", {
        panel: {
            width: 100,
            height: 100,
        },
    }, []);
}
