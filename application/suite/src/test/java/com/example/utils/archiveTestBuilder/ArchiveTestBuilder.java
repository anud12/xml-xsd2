package com.example.utils.archiveTestBuilder;

import com.example.utils.archiveTestBuilder.assertions.*;

import java.io.IOException;

public interface ArchiveTestBuilder extends
        ArchiveTestBuilderLifecycle,
        ArchiveTestBuilderSetup,
        ArchiveTestBuilderActions,
        AssertLogLines,
        AssertNotInLogs,
        AssertLogLineContains,
        AssertExportedStateEmpty,
        AssertExportedActions,
        AssertExportedModules,
        AssertExportedEntities,
        AssertExportedEvents,
        AssertExportedPanels,
        AssertPanelNames {

    static ArchiveTestBuilder create() throws IOException {
        var impl = new ArchiveTestBuilderImpl();
        impl.loadFeatureFilesFromCaller();
        return impl;
    }

    static ArchiveTestBuilder create(String resourcePath) throws IOException {
        var impl = new ArchiveTestBuilderImpl();
        impl.loadFeatureFiles(resourcePath);
        return impl;
    }
}
