class Booking {
  final String id;
  final String itemId;
  final String borrowerId;
  final DateTime requestedOn;
  final DateTime startDate;
  final DateTime endDate;
  final String status;
  final DateTime? approvedOn;
  final DateTime? completedOn;

  Booking({
    required this.id,
    required this.itemId,
    required this.borrowerId,
    required this.requestedOn,
    required this.startDate,
    required this.endDate,
    required this.status,
    this.approvedOn,
    this.completedOn,
  });

  factory Booking.fromJson(Map<String, dynamic> json) {
    return Booking(
      id: json['id'],
      itemId: json['itemId'],
      borrowerId: json['borrowerId'],
      requestedOn: DateTime.parse(json['requestedOn']),
      startDate: DateTime.parse(json['startDate']),
      endDate: DateTime.parse(json['endDate']),
      status: json['status'],
      approvedOn:
      json['approvedOn'] != null ? DateTime.parse(json['approvedOn']) : null,
      completedOn: json['completedOn'] != null
          ? DateTime.parse(json['completedOn'])
          : null,
    );
  }
}
