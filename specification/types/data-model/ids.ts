/**
 * Opaque identifier for an Entity, unique within a world_step.
 *
 * Used to reference entities across containers, filters, and wire messages.
 * Branded to prevent accidental substitution with ContainerIds or plain strings.
 *
 * @see entities.md
 */
export type UniqueGlobalEntityId = string & { readonly __brand: 'UniqueGlobalEntityId' };

/**
 * Opaque identifier for a Container, unique within a world_step.
 *
 * Used to reference containers in entity memberships, action targets, and
 * filter predicates. Branded to prevent accidental substitution with EntityIds
 * or plain strings.
 *
 * @see containers.md
 */
export type UniqueGlobalContainerId = string & { readonly __brand: 'UniqueGlobalContainerId' };

/**
 * A reference to a container by its global id.
 *
 * Used inside entity builders and data-model structures to express container
 * membership without embedding the full container object.
 */
export type ContainerReference = {
  /** The id of the referenced container. */
  containerIdRef: UniqueGlobalContainerId;
};
