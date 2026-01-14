# Rust Backend

This is a partial rewrite of the UniShare Backend in Rust.

## Structure

- `src/main.rs`: Entry point, server setup, database connection.
- `src/models.rs`: Data models (Booking, Item) mirroring the C# Entities.
- `src/handlers.rs`: API Endpoints (Controllers).
- `src/db.rs`: Database utilities.

## Prerequisites

- Rust (latest stable)
- PostgreSQL

## Running

1. Ensure `DATABASE_URL` is set in `.env` or environment.
2. Run `cargo run`.

## Endpoints Implemented

- `GET /api/bookings`
- `POST /api/bookings`
- `GET /api/items`
- `POST /api/items`

## Notes

- Authentication is currently stubbed (using random UUIDs for User IDs).
- Error handling is basic.
- Database schema is expected to match the existing one (tables "Bookings", "Items" with PascalCase columns).
