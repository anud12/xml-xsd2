/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string, number, condition, maybe} = hostApi.runtime;
  const sub = hostApi.runtime.setEntity({id: string.of('sub'), stringMap: {job: string.of('smith')}});
  const child = hostApi.runtime.setEntity({id: string.of('child'), stringMap: {job: string.of('smith')}, numberMap: {hp: number.of(3)}, entityListMap: {sub: [sub]}});
  const other = hostApi.runtime.setEntity({id: string.of('other'), stringMap: {job: string.of('healer')}, numberMap: {hp: number.of(8)}});
  const child2 = hostApi.runtime.setEntity({id: string.of('child2'), stringMap: {job: string.of('smith')}, numberMap: {hp: number.of(9)}});
  // Deliberately reference an unregistered id to exercise dangling relationships.
  /** @type {any} */
  const danglingId = string.of('does-not-exist');
  hostApi.runtime.setEntity({id: string.of('parent'), stringMap: {flag: string.of('no'), noMatch: string.of('no'), dangling: string.of('no'), projection: string.of('no'), recursive: string.of('no'), malformed: string.of('no'), shortCircuit: string.of('no'), emptyProjection: string.of('no')}, entityListMap: {items: [child, other, child2], empty: [], dangling: [danglingId], mixed: [danglingId, child]}});
  const smith = {filters: [e => e.string_map.get('job').map(t => t.isContainingExactly(string.of('smith'))).orElse(condition.of(false))]};
  const low = {filters: [e => e.number_map.get('hp').map(n => n.isLessOrEqualTo(number.of(5))).orElse(condition.of(false))]};
  const effect = hostApi.runtime.setEffect({
    name: string.of('members-check'), prepare: () => ({value: true}),
    apply: ctx => {
      ctx.getMutableEntityBy({and: [
        {filters: [e => e.id.isContainingExactly(string.of('parent'))]},
        {filters: [e => e.members('items').hasEntity(smith), e => e.members('items').hasEntity(low)]},
        {not: [{filters: [e => e.members('missing').hasEntity(smith)]}, {filters: [e => e.members('empty').hasEntity(smith)]}]},
        {or: [{filters: [e => e.members('items').findBy(smith).findBy(low).count().isEqualTo(number.of(1))]}]}
      ]}).map(e => e.string_map.put('flag', () => maybe.of(string.of('yes'))));
    }
  });
  hostApi.runtime.emitEvent(effect, {});

  const absent = {filters: [e => e.string_map.get('job').map(t => t.isContainingExactly(string.of('wizard'))).orElse(condition.of(false))]};
  /** @type {any} */
  const malformed = {unknown: true};
  /** @type {Record<string, import('module_types/EntityFilter').EntityFilter>} */
  const checks = {
    noMatch: {and: [
      {not: [{filters: [e => e.members('items').hasEntity(absent)]}]},
      {filters: [e => e.members('items').findBy(absent).count().isEqualTo(number.of(0))]},
    ]},
    dangling: {and: [
      {not: [{filters: [e => e.members('dangling').hasEntity({filters: []})]}]},
      {filters: [e => e.members('dangling').count().isEqualTo(number.of(1)),
        e => e.members('mixed').count().isEqualTo(number.of(2)),
        e => e.members('dangling').findBy({filters: []}).count().isEqualTo(number.of(0)),
        e => e.members('mixed').findBy({filters: []}).count().isEqualTo(number.of(1)),
        e => e.members('mixed').hasEntity(smith)]},
    ]},
    projection: {filters: [e => {
      const found = e.members('items').findBy(smith);
      const ids = [];
      found.forEach(member => ids.push(String(member.id)));
      return condition.of(ids.join(',') === 'child,child2');
    }, e => e.members('items').findBy(smith).count().isEqualTo(number.of(2))]},
    recursive: {filters: [e => e.members('items').hasEntity({filters: [c => c.members('sub').hasEntity(smith)]})]},
    malformed: {and: [
      {not: [{filters: [e => e.members('items').hasEntity(malformed)]}]},
      {filters: [e => e.members('items').findBy(malformed).count().isEqualTo(number.of(0))]},
    ]},
    shortCircuit: {filters: [e => {
      let visited = 0;
      e.members('items').hasEntity({filters: [() => { visited++; return condition.of(true); }]});
      let projected = 0;
      e.members('items').findBy({filters: [() => { projected++; return condition.of(true); }]});
      return condition.of(visited === 1 && projected === 3);
    }]},
    emptyProjection: {filters: [e => e.members('missing').findBy(smith).count().isEqualTo(number.of(0)),
      e => e.members('empty').findBy(smith).count().isEqualTo(number.of(0))]},
  };
  Object.entries(checks).forEach(([key, check]) => {
    const mark = hostApi.runtime.setEffect({
      name: string.of('members-' + key), prepare: () => ({value: true}),
      apply: ctx => ctx.getMutableEntityBy({and: [
        {filters: [e => e.id.isContainingExactly(string.of('parent'))]}, check,
      ]}).map(e => e.string_map.put(key, () => maybe.of(string.of('yes')))),
    });
    hostApi.runtime.emitEvent(mark, {});
  });
};
