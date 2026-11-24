export default (hostApi) => {
  const target = { x: 100, y: 100, width: 100, height: 80, title: 'Target' };
  hostApi.ui.window('target', { panel: target }, []);
  hostApi.ui.window('linked', { panel: { width: 60, height: 40, title: 'Linked', position: {
    link: { target: 'target', anchor: 'bottom-right', selfAnchor: 'top-left', offset: { x: 7, y: 9 } }
  } } }, []);
  hostApi.ui.window('cursor', { panel: { width: 40, height: 40, title: 'Cursor', position: { cursor: { offset: { x: 10, y: 10 } } } } }, []);
  hostApi.ui.window('parent', { panel: { x: 450, y: 100, width: 180, height: 160 } }, [
    hostApi.ui.window('missing', { panel: { width: 30, height: 30, title: 'Missing', position: {
      link: { target: 'absent', offset: { x: 12, y: 14 } }
    } } }, []),
    hostApi.ui.text('parent-label', 'Parent')
  ]);
  hostApi.ui.window('move', { panel: { x: 20, y: 300, width: 60, height: 40, onClick: () => { target.x += 30; target.y += 20; } } }, [hostApi.ui.text('move-label', 'Move')]);
  hostApi.ui.window('resize', { panel: { x: 100, y: 300, width: 60, height: 40, onClick: () => { target.width += 40; target.height += 25; } } }, [hostApi.ui.text('resize-label', 'Resize')]);
};
