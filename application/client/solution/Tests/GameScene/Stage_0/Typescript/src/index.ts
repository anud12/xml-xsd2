/**
 * Builds the UI for the stage module. Each visual element is defined in its
 * own file so the module source spans multiple TypeScript files, just like a
 * real module. The compiled JavaScript for every file lands next to each
 * other in the `module/` folder, ready to be loaded by the runtime.
 */
import type { ModuleEntrypoint } from "suite/types/ModuleEntrypoint";
import { buildBasePanel } from "./panels/base";
import { buildOffsetPanel } from "./panels/offset";

const moduleEntrypoint: ModuleEntrypoint = (hostApi) => {
  buildBasePanel(hostApi);
  buildOffsetPanel(hostApi);
}

export default moduleEntrypoint
