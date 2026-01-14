use serde::{Deserialize, Serialize};
use sqlx::FromRow;
use uuid::Uuid;
use chrono::{DateTime, Utc};

#[derive(Debug, Clone, Serialize, Deserialize, sqlx::Type)]
#[sqlx(type_name = "varchar", rename_all = "PascalCase")]
pub enum BookingStatus {
    Pending,
    Approved,
    Rejected,
    Completed,
    Canceled,
}

#[derive(Debug, Serialize, Deserialize, FromRow)]
pub struct Booking {
    pub id: Uuid,
    #[sqlx(rename = "ItemId")]
    pub item_id: Uuid,
    #[sqlx(rename = "BorrowerId")]
    pub borrower_id: Uuid,
    #[sqlx(rename = "RequestedOn")]
    pub requested_on: DateTime<Utc>,
    #[sqlx(rename = "StartDate")]
    pub start_date: DateTime<Utc>,
    #[sqlx(rename = "EndDate")]
    pub end_date: DateTime<Utc>,
    #[sqlx(rename = "BookingStatus")]
    pub booking_status: BookingStatus, // Mapped to string in DB
    #[sqlx(rename = "ApprovedOn")]
    pub approved_on: Option<DateTime<Utc>>,
    #[sqlx(rename = "CompletedOn")]
    pub completed_on: Option<DateTime<Utc>>,
    #[sqlx(rename = "IsPaid")]
    pub is_paid: bool,
}

#[derive(Debug, Deserialize)]
pub struct CreateBookingDto {
    pub item_id: Uuid,
    pub start_date: DateTime<Utc>,
    pub end_date: DateTime<Utc>,
}

#[derive(Debug, Clone, Serialize, Deserialize, sqlx::Type)]
#[sqlx(type_name = "varchar", rename_all = "PascalCase")]
pub enum ItemCategory {
    Others,
    Books,
    Electronics,
    Kitchen,
    Clothing,
    Accessories,
}

#[derive(Debug, Serialize, Deserialize, FromRow)]
pub struct Item {
    pub id: Uuid,
    #[sqlx(rename = "OwnerId")]
    pub owner_id: Uuid,
    #[sqlx(rename = "Name")]
    pub name: String,
    #[sqlx(rename = "Description")]
    pub description: String,
    #[sqlx(rename = "Category")]
    pub category: ItemCategory,
}

#[derive(Debug, Deserialize)]
pub struct CreateItemDto {
    pub name: String,
    pub description: String,
    pub category: ItemCategory,
}
