import type { HostApi } from "suite/types/HostApi";

/**
 * Registers the base panel at the viewport center.
 * The panel is 100x100 and has no offset, so it sits at (500, 500).
 */
export function buildBasePanel(hostApi: HostApi) {
  hostApi.ui.panel("base", {
    width: 100,
    height: 100,
  }, [])
}
