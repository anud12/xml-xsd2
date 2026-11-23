export default (hostApi) => {
  const { string, number } = hostApi.runtime;
  hostApi.runtime.setAction({
    name: string.of('denied'),
    guard: () => ({ condition: number.of(4).isGreaterThan(number.of(9)), message: string.of('Cannot buy: cost ').concat(number.of(9).toStringExpression()).concat(string.of(', you have ')).concat(number.of(4).toStringExpression()) }),
    apply: ctx => ctx.wait(10),
  });
  hostApi.runtime.setAction({ name: string.of('composed'), guard: () => ({ condition: false, message: string.of('').join([string.of('a'), string.of('b')], string.of('-')).prefix('[').suffix(']') }), apply: ctx => ctx.wait(10) });
  hostApi.runtime.setAction({ name: string.of('boundary'), guard: () => ({ condition: number.of(9).isGreaterThan(number.of(9)), message: string.of('equal') }), apply: ctx => ctx.wait(10) });
  const messageEffect = hostApi.runtime.setEffect({ name: string.of('message-effect'), apply: () => hostApi.ui.appendErrorMessage('effect-message') });
  hostApi.runtime.setAction({ name: string.of('emit-message-effect'), apply: ctx => ctx.emitEffect(messageEffect, {}) });
  hostApi.runtime.setAction({ name: string.of('append'), apply: ctx => { hostApi.ui.appendErrorMessage('appended-1'); hostApi.ui.appendErrorMessage('appended-2'); ctx.wait(10); } });
};
