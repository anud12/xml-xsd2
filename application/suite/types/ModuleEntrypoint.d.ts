import {HostApi} from "./HostApi";

export type ModuleEntrypoint = (hostApi:HostApi) => void;