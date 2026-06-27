// Concatenates the 3 scheduled context JS string parts

pub fn get_scheduled_ctx_js() -> String {
    format!("{}{}{}",
        super::scheduled_ctx_p1::get_part1(),
        super::scheduled_ctx_p2::get_part2(),
        super::scheduled_ctx_p3::get_part3())
}
