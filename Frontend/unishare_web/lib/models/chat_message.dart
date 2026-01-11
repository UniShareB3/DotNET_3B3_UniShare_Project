enum MessageType { text, image, document }

class ChatMessage {
  final String senderId;
  final String content;
  final String? imageUrl;
  String? documentUrl; // SAS URL for viewing (mutable to allow updating after bulk fetch)
  final String? blobName; // Original blob name from backend
  final String? documentName; // Original file name
  final MessageType messageType;
  final DateTime timestamp;

  ChatMessage({
    required this.senderId,
    required this.content,
    this.imageUrl,
    this.documentUrl,
    this.blobName,
    this.documentName,
    this.messageType = MessageType.text,
    required this.timestamp,
  });

  bool get isImage => messageType == MessageType.image;
  bool get isDocument => messageType == MessageType.document;
  
  // Check if documentUrl is a full URL (already resolved SAS URL)
  bool get isDocumentUrlComplete {
    if (documentUrl == null) return false;
    return documentUrl!.startsWith('http://') || documentUrl!.startsWith('https://');
  }

  // Check if this message needs its document URL to be resolved
  bool get needsUrlResolution {
    return (isImage || isDocument) && blobName != null && !isDocumentUrlComplete;
  }

  // Get the effective URL for display (documentUrl if complete, otherwise needs fetching)
  String? get effectiveDocumentUrl => isDocumentUrlComplete ? documentUrl : null;
  
  // Get file extension from document name
  String? get fileExtension {
    if (documentName == null) return null;
    final parts = documentName!.split('.');
    return parts.length > 1 ? parts.last.toLowerCase() : null;
  }

  factory ChatMessage.fromJson(Map<String, dynamic> json) {
    print('📥 ChatMessage.fromJson: Parsing message: $json');
    
    // Parse message type from contentType field
    MessageType type = MessageType.text;
    final contentType = json['contentType']?.toString().toLowerCase() ?? '';
    final typeStr = json['messageType']?.toString().toLowerCase() ?? '';
    
    // Check contentType first (new backend format)
    if (contentType.isNotEmpty) {
      if (contentType.startsWith('image/')) {
        type = MessageType.image;
        print('📥 ChatMessage: Detected as IMAGE (contentType: $contentType)');
      } else if (contentType.isNotEmpty && contentType != '') {
        // Any other content type means it's a document
        type = MessageType.document;
        print('📥 ChatMessage: Detected as DOCUMENT (contentType: $contentType)');
      }
    } 
    // Fallback to messageType field (old format)
    else if (typeStr == 'image' || typeStr == '1') {
      type = MessageType.image;
      print('📥 ChatMessage: Detected as IMAGE (messageType: $typeStr)');
    } else if (typeStr == 'document' || typeStr == '2') {
      type = MessageType.document;
      print('📥 ChatMessage: Detected as DOCUMENT (messageType: $typeStr)');
    }

    // Extract document name from blobName if available
    String? documentName;
    final blobNameValue = json['blobName']?.toString();
    if (blobNameValue != null && blobNameValue.isNotEmpty) {
      // Try to extract filename from blob path
      if (blobNameValue.contains('/')) {
        final parts = blobNameValue.split('/');
        documentName = parts.last;
      } else {
        documentName = blobNameValue;
      }
      print('📥 ChatMessage: Extracted documentName: $documentName from blobName: $blobNameValue');
    }

    // Use documentName from JSON if explicitly provided
    if (json['documentName'] != null) {
      documentName = json['documentName']?.toString();
      print('📥 ChatMessage: Using explicit documentName: $documentName');
    }

    // Get documentUrl if provided (from SignalR broadcast)
    final documentUrlValue = json['documentUrl']?.toString();
    if (documentUrlValue != null && documentUrlValue.isNotEmpty) {
      print('📥 ChatMessage: Found documentUrl in JSON: $documentUrlValue');
    }

    final message = ChatMessage(
      senderId: json['senderId']?.toString() ?? '',
      content: json['content'] ?? '',
      imageUrl: json['imageUrl'], // Legacy field
      documentUrl: documentUrlValue, // Use provided URL if available
      blobName: blobNameValue,
      documentName: documentName,
      messageType: type,
      timestamp: json['timestamp'] != null
          ? DateTime.parse(json['timestamp'])
          : DateTime.now(),
    );
    
    print('📥 ChatMessage: Final type=${message.messageType}, isImage=${message.isImage}, isDocument=${message.isDocument}, isDocumentUrlComplete=${message.isDocumentUrlComplete}');
    
    return message;
  }

  Map<String, dynamic> toJson() {
    return {
      'senderId': senderId,
      'content': content,
      'imageUrl': imageUrl,
      'documentUrl': documentUrl,
      'blobName': blobName,
      'documentName': documentName,
      'messageType': messageType == MessageType.image
          ? 'Image'
          : messageType == MessageType.document
              ? 'Document'
              : 'Text',
      'timestamp': timestamp.toIso8601String(),
    };
  }
}
