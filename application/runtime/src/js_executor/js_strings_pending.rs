// Concatenates the 3 pending context JS string parts

pub fn get_pending_ctx_js() -> String {
    format!("{}{}{}",
        super::pending_ctx_p1::get_part1(),
        super::pending_ctx_p2::get_part2(),
        super::pending_ctx_p3::get_part3())
}
