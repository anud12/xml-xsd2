/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string, number, condition, maybe} = hostApi.runtime;
  const item = hostApi.runtime.setEntity({id: string.of('item')});
  const candidate = hostApi.runtime.setEntity({id: string.of('candidate'), entityListMap: {items: [item]}});
  hostApi.runtime.setEntity({id: string.of('unlisted')});
  hostApi.runtime.setEntity({id: string.of('z'), entityListMap: {other: [candidate]}});
  hostApi.runtime.setEntity({id: string.of('3'), stringMap: {kind: string.of('map')}, numberMap: {rank: number.of(3)}, entityListMap: {members: [candidate], membersExtra: [candidate]}});
  hostApi.runtime.setEntity({id: string.of('1'), entityListMap: {members: [candidate]}});
  const members = key => key.isContainingExactly(string.of('members'));
  const any = () => condition.of(true);
  const ids = list => { const result = []; list.forEach(e => result.push(String(e.id))); return result.join(','); };
  const owner = {filters: [o => o.id.isContainingExactly(string.of('3')),
    o => o.string_map.get('kind').map(v => v.isContainingExactly(string.of('map'))).orElse(condition.of(false)),
    o => o.number_map.get('rank').map(v => v.isEqualTo(number.of(3))).orElse(condition.of(false))]};
  /** @type {Record<string, import('module_types/EntityFilter').EntityFilter>} */
  const checks = {
    match: {filters: [e => condition.of(ids(e.parentsInKey(members)) === '1,3')]},
    noMatch: {filters: [e => e.parentsInKey(k => k.isContainingExactly(string.of('missing'))).count().isEqualTo(number.of(0))]},
    unlisted: {filters: [e => e.parentsInKey(any).count().isEqualTo(number.of(0))]},
    any: {filters: [e => condition.of(ids(e.parentsInKey(any)) === '1,3,z')]},
    dedup: {filters: [e => e.parentsInKey(k => k.isContaining(string.of('members'))).count().isEqualTo(number.of(2))]},
    ordering: {filters: [e => condition.of(ids(e.parentsInKey(any)) === '1,3,z')]},
    has: {filters: [e => e.parentsInKey(members).hasEntity(owner)]},
    find: {filters: [e => { const found = e.parentsInKey(any).findBy(owner); return condition.of(ids(found) === '3'); },
      e => e.parentsInKey(any).findBy(owner).count().isEqualTo(number.of(1))]},
    malformed: {filters: [e => {
      /** @type {any} */ const bad = () => ({invalid: true});
      return e.parentsInKey(bad).count().isEqualTo(number.of(0));
    }, e => e.parentsInKey(() => { throw new Error('test'); }).count().isEqualTo(number.of(0)),
    e => condition.of(ids(e.parentsInKey(k => { if (String(k) === 'members') throw new Error('test'); return k.isContainingExactly(string.of('membersExtra')); })) === '3')]},
    composition: {and: [
      {filters: [e => e.parentsInKey(k => k.isContainingExactly(string.of('members')))
        .hasEntity({filters: [o => o.id.isContainingExactly(string.of('3'))]})]},
      {filters: [e => e.members('items').hasEntity({filters: [() => condition.of(true)]})]},
    ]},
  };
  Object.entries(checks).forEach(([key, check]) => {
    const effect = hostApi.runtime.setEffect({name: string.of('parents-' + key), prepare: () => ({value: true}),
      apply: ctx => ctx.getMutableEntityBy({and: [
        {filters: [e => e.id.isContainingExactly(string.of(key === 'unlisted' ? 'unlisted' : 'candidate'))]}, check,
      ]}).map(e => e.string_map.put(key, () => maybe.of(string.of('yes')))),
    });
    hostApi.runtime.emitEvent(effect, {});
  });
};
