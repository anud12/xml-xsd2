export default (hostApi) => {
  let armed = false;
  const panel = { x: 100, y: 100, width: 120, height: 100,
    onHover: { window: () => armed ? { options: { panel: { width: 60, height: 40, title: 'Tooltip',
      position: { cursor: { offset: { x: 12, y: 12 } } } } }, children: [hostApi.ui.text('tooltip-label', 'Visible')] } : null } };
  hostApi.ui.window('source', { panel }, [hostApi.ui.text('source-label', 'Source')]);
  hostApi.ui.window('toggle', { panel: { x: 300, y: 100, width: 60, height: 40, onClick: () => {
    armed = !armed;
    panel.height = armed ? 101 : 100;
  } } }, [hostApi.ui.text('toggle-label', 'Toggle')]);
};
