package com.example.utils;

import java.io.IOException;

public interface ArchiveTestBuilder extends 
    ArchiveTestBuilderLifecycle,
    ArchiveTestBuilderSetup,
    ArchiveTestBuilderActions,
    ArchiveTestBuilderAssertions {

    static ArchiveTestBuilder create() {
        return new ArchiveTestBuilderImpl();
    }

    static ArchiveTestBuilder create(String resourcePath) throws IOException {
        var impl = new ArchiveTestBuilderImpl();
        impl.loadFeatureFiles(resourcePath);
        return impl;
    }
}
