use xml_xsd2::js_executor::extract_from_source;

#[test]
fn extract_basic_declarations() {
    let src = r#"
    // register a named event
    host.registerEvent({ name: "user.created" });

    // create an entity
    createEntity({ firstName: "Alice", lastName: "Smith" });

    // top-level function
    function helper() { return 1; }
    "#;

    let dec = extract_from_source(src).expect("extract");
    assert!(dec.events.contains(&"user.created".to_string()));
    assert!(dec.entities.contains(&"Alice".to_string()));
    assert!(dec.functions.iter().any(|f| f == "helper"));
}
