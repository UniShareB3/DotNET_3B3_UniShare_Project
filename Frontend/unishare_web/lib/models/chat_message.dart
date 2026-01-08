enum MessageType { text, image }

class ChatMessage {
  final String senderId;
  final String content;
  final String? imageUrl;
  final MessageType messageType;
  final DateTime timestamp;

  ChatMessage({
    required this.senderId,
    required this.content,
    this.imageUrl,
    this.messageType = MessageType.text,
    required this.timestamp,
  });

  bool get isImage => messageType == MessageType.image;

  factory ChatMessage.fromJson(Map<String, dynamic> json) {
    MessageType type = MessageType.text;
    final typeStr = json['messageType']?.toString().toLowerCase();
    if (typeStr == 'image' || typeStr == '1') {
      type = MessageType.image;
    }

    return ChatMessage(
      senderId: json['senderId']?.toString() ?? '',
      content: json['content'] ?? '',
      imageUrl: json['imageUrl'],
      messageType: type,
      timestamp: json['timestamp'] != null
          ? DateTime.parse(json['timestamp'])
          : DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'senderId': senderId,
      'content': content,
      'imageUrl': imageUrl,
      'messageType': messageType == MessageType.image ? 'Image' : 'Text',
      'timestamp': timestamp.toIso8601String(),
    };
  }
}

