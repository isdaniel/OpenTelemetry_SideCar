FROM rust:1.85 as builder

WORKDIR /usr/src/app
COPY . .

# Build with release optimizations
RUN cargo build --release

# Runtime stage
FROM debian:bookworm-slim

# Install OpenSSL and CA certificates for HTTPS requests
RUN apt-get update && apt-get install -y \
    libssl-dev \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy the binary from the builder stage
COPY --from=builder /usr/src/app/target/release/rust_web /app/

# Expose the port the app runs on
EXPOSE 3333

# Run the binary
CMD ["./rust_web"]