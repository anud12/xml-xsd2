use super::{JsPanel, Anchor, Size, ParsedFields};

pub(crate) fn parse_fields(
    parsed: &JsPanel
) -> ParsedFields {
    let a = parsed.anchor.as_ref().cloned().unwrap_or(
        Anchor {
            x: None, y: None,
            top: None, bottom: None,
            left: None, right: None,
        }
    );
    let pv = parsed.pivot.as_ref().cloned().unwrap_or(
        Anchor {
            x: None, y: None,
            top: None, bottom: None,
            left: None, right: None,
        }
    );
    let of = parsed.offset.as_ref().cloned().unwrap_or(
        Anchor {
            x: None, y: None,
            top: None, bottom: None,
            left: None, right: None,
        }
    );
    let sz = parsed.size.as_ref().cloned().unwrap_or(
        Size { height: 100.0, width: 100.0 }
    );
    ParsedFields {
        ax: a.x.unwrap_or(0.0),
        ay: a.y.unwrap_or(0.0),
        px: pv.x.unwrap_or(0.0),
        py: pv.y.unwrap_or(0.0),
        ot: of.top.unwrap_or(of.y.unwrap_or(0.0)),
        ob: of.bottom.unwrap_or(0.0),
        ol: of.left.unwrap_or(of.x.unwrap_or(0.0)),
        or_val: of.right.unwrap_or(0.0),
        sh: sz.height,
        sw: sz.width,
    }
}
