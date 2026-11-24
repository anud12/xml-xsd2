export default (hostApi) => {
  const { number, string } = hostApi.runtime;
  const hover = hostApi.runtime.setAnimation(string.of('hover'), {
    frames: [{ sprite: hostApi.ui.getSpritePNG('hover.png') }], duration: number.of(1)
  });
  hostApi.ui.window('hover-parent', { panel: { x: 100, y: 100, width: 140, height: 140, title: 'Parent' } }, [
  hostApi.ui.window('hover-source', { panel: {
    x: 10, y: 10, width: 100, height: 100, title: 'Source',
    onHover: {
      texture: { animation: hover, fill: { width: 'stretch', height: 'stretch' } }, thickness: 10,
      window: (ctx) => ctx.node.id === 'hover-source' && ctx.window.id === 'hover-source'
        ? ({ options: { panel: { x: 300, y: 300, width: 80, height: 60, title: 'Pinned', pinnable: { key: 'P' } } }, children: [hostApi.ui.text('tooltip-label', 'Visible')] }) : null
    }
  } }, [])]);
};
