# Runtime Docker image: Godot v4.6.3 + GDUnit4 test runner
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base

# Godot version
ARG GODOT_VERSION=4.6.3
ENV GODOT_VERSION=${GODOT_VERSION}

# Install system dependencies + Adoptium JDK 21 + Maven
RUN apt-get update && apt-get install -y --no-install-recommends \
    wget \
    unzip \
    cabextract \
    libglx0 \
    libxrandr2 \
    libxinerama1 \
    libxcursor1 \
    libxi6 \
    libxext6 \
    libasound2 \
    libpulse0 \
    libsdl2-2.0-0 \
    xvfb \
    curl \
    libsqlite3-dev \
    pkg-config \
    gcc \
    ca-certificates \
    gnupg \
    && install -m 0755 -d /etc/apt/keyrings && \
    wget -qO - https://packages.adoptium.net/artifactory/api/gpg/key/public | gpg --dearmor -o /etc/apt/keyrings/adoptium.gpg && \
    echo "deb [signed-by=/etc/apt/keyrings/adoptium.gpg] https://packages.adoptium.net/artifactory/deb $(cat /etc/os-release | grep ^VERSION_CODENAME= | cut -d= -f2) main" > /etc/apt/sources.list.d/adoptium.list && \
    apt-get update && apt-get install -y --no-install-recommends \
    temurin-21-jdk \
    maven \
    && rm -rf /var/lib/apt/lists/*

# Install Rust
RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y && \
    ln -sf /root/.cargo/bin/cargo /usr/local/bin/cargo && \
    ln -sf /root/.cargo/bin/rustc /usr/local/bin/rustc

# Download and install Godot v4.6.3 stable mono (Linux x86_64)
RUN wget -q "https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip" \
    -O /tmp/godot.zip \
    && unzip -q /tmp/godot.zip -d /opt/godot \
    && chmod +x /opt/godot/Godot_* \
    && rm /tmp/godot.zip

# Environment variables for Godot
ENV GODOT_BIN="/opt/godot/Godot_v4.6.3-stable_mono_linux_x86_64/Godot_v4.6.3-stable_mono_linux.x86_64" \
    DISPLAY=":99" \
    PATH="/root/.cargo/bin:$PATH" \
    LD_LIBRARY_PATH="/workspace/application/client/solution/.godot/mono/temp/bin/Release:/workspace/application/client/solution/.godot/mono/temp/bin/Debug:/workspace/application/runtime/target/release:/workspace/application/suite/target/test-classes"

# Copy runtime, suite, and solution
WORKDIR /workspace
COPY application/ ./application/

# Build Rust library for Linux
WORKDIR /workspace/application/runtime
RUN mkdir -p target && \
    cargo build --release && \
    mkdir -p /workspace/application/suite/target/test-classes && \
    cp target/release/liblibxml_xsd2.so /workspace/application/suite/target/test-classes/libxml_xsd2.so && \
    mkdir -p /workspace/application/client/solution/.godot/mono/temp/bin/Release && \
    mkdir -p /workspace/application/client/solution/.godot/mono/temp/bin/Debug && \
    cp target/release/liblibxml_xsd2.so /workspace/application/client/solution/.godot/mono/temp/bin/Release/libxml_xsd2.so && \
    cp target/release/liblibxml_xsd2.so /workspace/application/client/solution/.godot/mono/temp/bin/Debug/libxml_xsd2.so

# Generate .runsettings with actual Godot path (in client/solution for dotnet test)
WORKDIR /workspace/application/client/solution
RUN sed -i "s|\$GODOT_BIN|$GODOT_BIN|g" .runsettings

# Restore and build C# project (skip Rust build — .so already copied)
RUN dotnet restore "New Game Project.csproj" && \
    dotnet build "New Game Project.csproj" \
      -c Release \
      --no-incremental \
      /p:SkipRustBuild=true \
      /p:SkipJsValidation=true

# Overwrite Debug DLL with Release so Godot runtime loads correct assembly
RUN cp ".godot/mono/temp/bin/Release/New Game Project.dll" ".godot/mono/temp/bin/Debug/New Game Project.dll" && \
    cp ".godot/mono/temp/bin/Release/New Game Project.pdb" ".godot/mono/temp/bin/Debug/New Game Project.pdb"

# Run all tests (Java + C#) via java_tests.rs, with Xvfb for headless Godot
ENTRYPOINT ["/bin/bash", "-c"]
CMD ["\
    Xvfb :99 -screen 0 1280x720x24 & \
    sleep 2 && \
    cd /workspace/application/runtime && \
    cargo run --bin java_tests \
    "]
