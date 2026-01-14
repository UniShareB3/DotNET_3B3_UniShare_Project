use actix_web::{web, HttpResponse, Responder, get, post};
use sqlx::PgPool;
use uuid::Uuid;
use chrono::Utc;
use crate::models::{Booking, BookingStatus, CreateBookingDto, Item, CreateItemDto};

pub fn config(cfg: &mut web::ServiceConfig) {
    cfg.service(
        web::scope("/bookings")
            .service(get_bookings)
            .service(create_booking)
    )
    .service(
        web::scope("/items")
            .service(get_items)
            .service(create_item)
    );
}

#[get("")]
async fn get_bookings(pool: web::Data<PgPool>) -> impl Responder {
    let result = sqlx::query_as::<_, Booking>(
        r#"
        SELECT * FROM "Bookings"
        "#
    )
    .fetch_all(pool.get_ref())
    .await;

    match result {
        Ok(bookings) => HttpResponse::Ok().json(bookings),
        Err(_) => HttpResponse::InternalServerError().finish(),
    }
}

#[post("")]
async fn create_booking(
    pool: web::Data<PgPool>,
    booking_dto: web::Json<CreateBookingDto>,
) -> impl Responder {
    let new_booking = Booking {
        id: Uuid::new_v4(),
        item_id: booking_dto.item_id,
        borrower_id: Uuid::new_v4(), // Placeholder for extracted User ID from token
        requested_on: Utc::now(),
        start_date: booking_dto.start_date,
        end_date: booking_dto.end_date,
        booking_status: BookingStatus::Pending,
        approved_on: None,
        completed_on: None,
        is_paid: false,
    };

    let result = sqlx::query(
        r#"
        INSERT INTO "Bookings" (
            "Id", "ItemId", "BorrowerId", "RequestedOn", "StartDate", "EndDate",
            "BookingStatus", "ApprovedOn", "CompletedOn", "IsPaid"
        )
        VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)
        "#
    )
    .bind(new_booking.id)
    .bind(new_booking.item_id)
    .bind(new_booking.borrower_id)
    .bind(new_booking.requested_on)
    .bind(new_booking.start_date)
    .bind(new_booking.end_date)
    .bind(&new_booking.booking_status)
    .bind(new_booking.approved_on)
    .bind(new_booking.completed_on)
    .bind(new_booking.is_paid)
    .execute(pool.get_ref())
    .await;

    match result {
        Ok(_) => HttpResponse::Created().json(new_booking),
        Err(e) => {
            eprintln!("Error creating booking: {:?}", e);
            HttpResponse::InternalServerError().body(format!("Error: {:?}", e))
        },
    }
}

#[get("")]
async fn get_items(pool: web::Data<PgPool>) -> impl Responder {
    let result = sqlx::query_as::<_, Item>(
        r#"
        SELECT * FROM "Items"
        "#
    )
    .fetch_all(pool.get_ref())
    .await;

    match result {
        Ok(items) => HttpResponse::Ok().json(items),
        Err(e) => {
            eprintln!("Error getting items: {:?}", e);
            HttpResponse::InternalServerError().finish()
        },
    }
}

#[post("")]
async fn create_item(
    pool: web::Data<PgPool>,
    item_dto: web::Json<CreateItemDto>,
) -> impl Responder {
    let new_item = Item {
        id: Uuid::new_v4(),
        owner_id: Uuid::new_v4(), // Placeholder
        name: item_dto.name.clone(),
        description: item_dto.description.clone(),
        category: item_dto.category.clone(),
    };

    let result = sqlx::query(
        r#"
        INSERT INTO "Items" ("Id", "OwnerId", "Name", "Description", "Category")
        VALUES ($1, $2, $3, $4, $5)
        "#
    )
    .bind(new_item.id)
    .bind(new_item.owner_id)
    .bind(&new_item.name)
    .bind(&new_item.description)
    .bind(&new_item.category)
    .execute(pool.get_ref())
    .await;

    match result {
        Ok(_) => HttpResponse::Created().json(new_item),
        Err(e) => {
            eprintln!("Error creating item: {:?}", e);
            HttpResponse::InternalServerError().body(format!("Error: {:?}", e))
        },
    }
}
