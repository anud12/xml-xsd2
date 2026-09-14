import type { HostApi } from "suite/types/HostApi";

/**
 * Registers an offset panel displaced from the viewport center.
 * The panel is 50x50 and sits at (20, 30) in parent space.
 */
export function buildOffsetPanel(hostApi: HostApi) {
  hostApi.ui.panel("offset", {
    width: 50,
    height: 50,
    x: 20,
    y: 30,
  }, [])
}
