// Concatenates the 3 sim template JS string parts

pub fn sim_template_js() -> String {
    format!("{}{}{}",
        super::sim_tpl_p1::get_part1(),
        super::sim_tpl_p2::get_part2(),
        super::sim_tpl_p3::get_part3())
}
