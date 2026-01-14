FROM rust:1.75 as builder

WORKDIR /usr/src/unishare_backend
COPY . .
RUN cargo install --path .

FROM debian:bookworm-slim
RUN apt-get update && apt-get install -y libssl-dev ca-certificates && rm -rf /var/lib/apt/lists/*
COPY --from=builder /usr/local/cargo/bin/unishare_backend /usr/local/bin/unishare_backend
COPY .env .

CMD ["unishare_backend"]
