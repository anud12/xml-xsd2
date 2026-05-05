package com.example.utils.archiveTestBuilder.assertions;

import com.example.utils.StateAssertions;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilderCommon;

public interface AssertExportedStateEmpty extends ArchiveTestBuilderCommon {

    default ArchiveTestBuilder assertExportedStateEmpty() throws Exception {
        StateAssertions.assertExportedStateEmpty(this.getState());
        return (ArchiveTestBuilder) this;
    }
}
