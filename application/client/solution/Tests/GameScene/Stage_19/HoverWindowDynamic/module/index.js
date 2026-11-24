export default (hostApi) => {
  const panel = { x: 100, y: 100, width: 120, height: 100, onHover: { emitAction: 'source-hover' } };
  const callback = ctx => ctx.node.id === 'source' && ctx.window.id === 'source'
    && ctx.cursor.x === 110 && ctx.cursor.y === 110 && ctx.rect.width === 120
    ? { options: { panel: { width: 60, height: 40, title: 'Tooltip', position: { cursor: { offset: { x: 12, y: 12 } } } } }, children: [hostApi.ui.text('tooltip-label', 'Visible')] } : null;
  hostApi.ui.window('source', { panel }, [hostApi.ui.text('source-label', 'Source')]);
  hostApi.ui.window('unrelated', { panel: { x: 400, y: 100, width: 60, height: 40, onClick: () => {
    panel.height = panel.height === 100 ? 101 : 100;
  } } }, [hostApi.ui.text('other-label', 'Other')]);
  hostApi.ui.window('toggle', { panel: { x: 300, y: 100, width: 60, height: 40, onClick: () => {
    if (panel.onHover.window) delete panel.onHover.window;
    else panel.onHover.window = callback;
  } } }, [hostApi.ui.text('toggle-label', 'Toggle')]);
};
