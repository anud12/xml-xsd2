export type Declarations = {
  events: string[];
  actions: string[];
  functions: string[];
  entities: string[];
  creators: Record<string, string[]>;
  emits: Record<string, string[]>;
  logs: string[];
  panels: string[];
  entity_data: Record<string, unknown>;
};

export type PendingEffect = {
  name: string;
  payload: Record<string, unknown>;
};

export type CreatedEntity = {
  firstName?: string;
  textMap?: Record<string, string>;
  numberMap?: Record<string, number>;
};
