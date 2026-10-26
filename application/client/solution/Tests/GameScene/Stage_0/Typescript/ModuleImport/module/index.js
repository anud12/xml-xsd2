import { buildBasePanel } from "./panels/base";
import { buildOffsetPanel } from "./panels/offset";
const moduleEntrypoint = (hostApi) => {
    buildBasePanel(hostApi);
    buildOffsetPanel(hostApi);
};
export default moduleEntrypoint;
