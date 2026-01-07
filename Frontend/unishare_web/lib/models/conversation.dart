class Conversation {
  final String userId;
  final String userName;
  final String? userEmail;
  final String? lastMessage;
  final DateTime? lastMessageTime;
  final String? lastMessageSenderId;

  Conversation({
    required this.userId,
    required this.userName,
    this.userEmail,
    this.lastMessage,
    this.lastMessageTime,
    this.lastMessageSenderId,
  });

  factory Conversation.fromJson(Map<String, dynamic> json) {
    return Conversation(
      userId: json['userId']?.toString() ?? '',
      userName: json['userName'] ?? 'Unknown User',
      userEmail: json['userEmail'],
      lastMessage: json['lastMessage'],
      lastMessageTime: json['lastMessageTime'] != null
          ? DateTime.parse(json['lastMessageTime'])
          : null,
      lastMessageSenderId: json['lastMessageSenderId']?.toString(),
    );
  }
}

