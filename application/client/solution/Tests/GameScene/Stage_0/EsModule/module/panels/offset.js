export function buildOffsetPanel(hostApi) {
    hostApi.ui.window("offset", {
        panel: {
            width: 50,
            height: 50,
            x: 20,
            y: 30,
        },
    }, []);
}
