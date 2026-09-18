import { buildBasePanel } from "./panels/base";
import { buildOffsetPanel } from "./panels/offset";

export default (hostApi) => {
    buildBasePanel(hostApi);
    buildOffsetPanel(hostApi);
};
