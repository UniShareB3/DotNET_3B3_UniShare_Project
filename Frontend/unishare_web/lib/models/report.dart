class Report {
  final String id;
  final String itemId;
  final String ownerId;
  final String userId;
  final String description;
  final DateTime createdDate;
  final String status; // PENDING, ACCEPTED, DECLINED
  final String? moderatorId;

  Report({
    required this.id,
    required this.itemId,
    required this.ownerId,
    required this.userId,
    required this.description,
    required this.createdDate,
    required this.status,
    this.moderatorId,
  });

  factory Report.fromJson(Map<String, dynamic> json) {
    return Report(
      id: json['id'],
      itemId: json['itemId'],
      ownerId: json['ownerId'],
      userId: json['userId'],
      description: json['description'],
      createdDate: DateTime.parse(json['createdDate']),
      status: json['status'],
      moderatorId: json['moderatorId'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'itemId': itemId,
      'ownerId': ownerId,
      'userId': userId,
      'description': description,
      'createdDate': createdDate.toIso8601String(),
      'status': status,
      'moderatorId': moderatorId,
    };
  }
}

class CreateReportDto {
  final String itemId;
  final String userId;
  final String description;

  CreateReportDto({
    required this.itemId,
    required this.userId,
    required this.description,
  });

  Map<String, dynamic> toJson() {
    return {
      'itemId': itemId,
      'userId': userId,
      'description': description,
    };
  }
}

class UpdateReportStatusDto {
  final String status; // PENDING, ACCEPTED, DECLINED
  final String moderatorId;

  UpdateReportStatusDto({
    required this.status,
    required this.moderatorId,
  });

  Map<String, dynamic> toJson() {
    return {
      'status': status,
      'moderatorId': moderatorId,
    };
  }
}

