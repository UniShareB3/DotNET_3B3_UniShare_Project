import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'secure_storage_service.dart';
import 'package:unishare_web/services/api_service.dart'; // use authenticated helpers

/// Service for managing chat functionality including SignalR real-time connection
/// and REST API calls for chat history
class ChatService {
  static final String baseUrl = dotenv.env['API_BASE_URL'] ?? 'http://localhost:5083';

  static HubConnection? _hubConnection;
  static bool _isConnecting = false;
  static final List<Function(Map<String, dynamic>)> _messageListeners = [];

  /// Get the SignalR hub connection, creating it if necessary
  static Future<HubConnection?> getConnection() async {
    if (_hubConnection != null &&
        _hubConnection!.state == HubConnectionState.Connected) {
      return _hubConnection;
    }

    if (_isConnecting) {
      // Wait for existing connection attempt
      await Future.delayed(const Duration(milliseconds: 500));
      return _hubConnection;
    }

    _isConnecting = true;

    try {
      final token = await SecureStorageService.getAccessToken();
      if (token == null || token.isEmpty) {
        print('❌ ChatService: No token available for SignalR connection');
        _isConnecting = false;
        return null;
      }

      final hubUrl = '$baseUrl/hubs/chat?access_token=$token';
      print('🔌 ChatService: Connecting to SignalR hub at $hubUrl');

      _hubConnection = HubConnectionBuilder()
          .withUrl(hubUrl)
          .withAutomaticReconnect()
          .build();

      // Set up message received handler
      _hubConnection!.on('ReceiveMessage', (arguments) {
        if (arguments != null && arguments.isNotEmpty) {
          final messageData = arguments[0] as Map<String, dynamic>;
          print('📩 ChatService: Received message: $messageData');

          // Notify all listeners
          for (var listener in _messageListeners) {
            listener(messageData);
          }
        }
      });

      // Handle connection state changes
      _hubConnection!.onclose(({error}) {
        print('🔌 ChatService: Connection closed. Error: $error');
      });

      _hubConnection!.onreconnecting(({error}) {
        print('🔄 ChatService: Reconnecting... Error: $error');
      });

      _hubConnection!.onreconnected(({connectionId}) {
        print('✅ ChatService: Reconnected with ID: $connectionId');
      });

      await _hubConnection!.start();
      print('✅ ChatService: Connected to SignalR hub');

      _isConnecting = false;
      return _hubConnection;
    } catch (e) {
      print('❌ ChatService: Failed to connect to SignalR hub: $e');
      _isConnecting = false;
      return null;
    }
  }

  /// Add a listener for incoming messages
  static void addMessageListener(Function(Map<String, dynamic>) listener) {
    _messageListeners.add(listener);
  }

  /// Remove a message listener
  static void removeMessageListener(Function(Map<String, dynamic>) listener) {
    _messageListeners.remove(listener);
  }

  /// Send a text message to another user via SignalR
  static Future<bool> sendMessage(String receiverId, String message) async {
    try {
      final connection = await getConnection();
      if (connection == null) {
        print('❌ ChatService: Cannot send message - not connected');
        return false;
      }

      await connection.invoke('SendMessage', args: [receiverId, message]);
      print('✅ ChatService: Message sent to $receiverId');
      return true;
    } catch (e) {
      print('❌ ChatService: Failed to send message: $e');
      return false;
    }
  }

    /// Send a document message to another user via SignalR
  /// This expects the blobPath (path in blob storage) and the resolved documentUrl (SAS URL)
  static Future<bool> sendDocumentMessage(String receiverId, String blobName, String documentUrl, {String? caption}) async {
    try {
      final connection = await getConnection();
      if (connection == null) {
        print('❌ ChatService: Cannot send document - not connected');
        return false;
      }

      await connection.invoke('SendImageMessage', args: [receiverId, blobName, documentUrl, caption ?? '']);
      print('✅ ChatService: Document sent to $receiverId');
      return true;
    } catch (e) {
      print('❌ ChatService: Failed to send document: $e');
      return false;
    }
  }

  /// Legacy method for backward compatibility
  @Deprecated('Use sendDocumentMessage instead')
  static Future<bool> sendImageMessage(String receiverId, String blobPath, String documentUrl, {String? caption}) async {
    return sendDocumentMessage(receiverId, blobPath, documentUrl, caption: caption);
  }

  /// Retrieve SAS URL for uploading a document to blob storage
  static Future<Map<String, dynamic>?> retrieveSasUrl(String fileName, String mimeType) async {
    try {
      final url = Uri.parse('$baseUrl/chat/documents/upload-url');
      final response = await ApiService.authenticatedPost(
        url,
        body: jsonEncode({
          'fileName': fileName,
          'contentType': mimeType,
        }),
      );

      print('🔗 ChatService: Get SAS URL status: ${response.statusCode}');

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        // Expected response: { "uploadUrl": "...", "blobName": "..." }
        return {
          'uploadUrl': data['uploadUrl'] as String,
          'blobName': data['blobName'] as String,
        };
      }

      print('❌ ChatService: Failed to get SAS URL: ${response.body}');
      return null;
    } catch (e) {
      print('❌ ChatService: Failed to retrieve SAS URL: $e');
      return null;
    }
  }

  /// Retrieve SAS URL for viewing/downloading a document from blob storage
  /// Uses POST endpoint with blobName in request body
  static Future<Map<String, dynamic>?> getDocumentViewUrl(String blobName) async {
    try {
      final url = Uri.parse('$baseUrl/chat/documents/view-url');
      final response = await ApiService.authenticatedPost(
        url,
        body: jsonEncode({
          'blobName': blobName,
        }),
      );

      print('🔗 ChatService: Get view URL status: ${response.statusCode}');

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        // Expected response: { "blobName": "...", "documentUrl": "https://...", "contentType": "...", "expiresAt": "..." }
        return {
          'blobName': data['blobName'] as String?,
          'documentUrl': data['documentUrl'] as String?,
          'contentType': data['contentType'] as String?,
          'expiresAt': data['expiresAt'] as String?,
        };
      }

      print('❌ ChatService: Failed to get view URL: ${response.body}');
      return null;
    } catch (e) {
      print('❌ ChatService: Failed to retrieve view URL: $e');
      return null;
    }
  }

  /// Retrieve SAS URLs for multiple documents in bulk
  /// Uses POST endpoint with list of blobNames in request body
  static Future<List<Map<String, dynamic>>?> getBulkDocumentViewUrls(List<String> blobNames) async {
    try {
      final url = Uri.parse('$baseUrl/chat/bulk/documents/url');
      final response = await ApiService.authenticatedPost(
        url,
        body: jsonEncode({
          'blobNames': blobNames,
        }),
      );

      print('🔗 ChatService: Get bulk view URLs status: ${response.statusCode}');

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        // Expected response: [{ "blobName": "...", "documentUrl": "https://...", "contentType": "...", "expiresAt": "..." }, ...]
        return data.map((item) => {
          'blobName': item['blobName'] as String?,
          'documentUrl': item['documentUrl'] as String?,
          'contentType': item['contentType'] as String?,
          'expiresAt': item['expiresAt'] as String?,
        }).toList();
      }

      print('❌ ChatService: Failed to get bulk view URLs: ${response.body}');
      return null;
    } catch (e) {
      print('❌ ChatService: Failed to retrieve bulk view URLs: $e');
      return null;
    }
  }

  /// Send confirmation to backend after successful upload to blob storage
  /// Returns the response data including DocumentUrl on success, null on failure
  static Future<Map<String, dynamic>?> sendConfirmationUploadRequest(String blobName, String receiverId) async {
    try {
      final url = Uri.parse('$baseUrl/chat/documents/confirm-upload');
      final response = await ApiService.authenticatedPost(
        url,
        body: jsonEncode({
          'blobName': blobName,
          'receiverId': receiverId,
        }),
      );

      print('✅ ChatService: Confirm upload status: ${response.statusCode}');

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        print('✅ ChatService: Upload confirmed successfully, DocumentUrl: ${data['documentUrl']}');
        return {
          'messageId': data['messageId'],
          'documentUrl': data['documentUrl'],
          'blobName': data['blobName'],
          'timestamp': data['timestamp'],
          'expiresAt': data['expiresAt'],
        };
      }

      print('❌ ChatService: Failed to confirm upload: ${response.body}');
      return null;
    } catch (e) {
      print('❌ ChatService: Failed to send confirmation: $e');
      return null;
    }
  }

  /// Upload a document directly to blob storage using SAS URL
  /// Returns a map with blobName and documentUrl on success, null on failure
  /// Supports images and other file types
  static Future<Map<String, dynamic>?> uploadDocument(List<int> fileBytes, String fileName, String receiverId) async {
    print('📤 ChatService: uploadDocument called with receiverId: "$receiverId", fileName: "$fileName"');
    
    if (receiverId.isEmpty) {
      print('⚠️ ChatService: WARNING - receiverId is empty!');
    }

    final extension = fileName.toLowerCase().split('.').last;

    String mimeType;
    switch (extension) {
    // Image types
      case 'jpg':
      case 'jpeg':
        mimeType = 'image/jpeg';
        break;
      case 'png':
      case 'gif':
      case 'webp':
        mimeType = 'image/png';
        break;
    // Document types
      case 'pdf':
        mimeType = 'application/pdf';
        break;
      case 'doc':
        mimeType = 'application/msword';
        break;
      case 'docx':
        mimeType = 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';
        break;
      case 'xls':
        mimeType = 'application/vnd.ms-excel';
        break;
      case 'xlsx':
        mimeType = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
        break;
      case 'ppt':
        mimeType = 'application/vnd.ms-powerpoint';
        break;
      case 'pptx':
        mimeType = 'application/vnd.openxmlformats-officedocument.presentationml.presentation';
        break;
      case 'txt':
        mimeType = 'text/plain';
        break;
      case 'csv':
        mimeType = 'text/csv';
        break;
      case 'zip':
        mimeType = 'application/zip';
        break;
      case 'rar':
        mimeType = 'application/x-rar-compressed';
        break;
      default:
      // Use generic binary type for unknown files
        mimeType = 'application/octet-stream';
        print('⚠️ ChatService: Unknown file type: $extension, using application/octet-stream');
    }

    try {
      // Step 1: Retrieve SAS URL and blob path
      final sasData = await retrieveSasUrl(fileName, mimeType);
      if (sasData == null) {
        print('❌ ChatService: Failed to retrieve SAS URL');
        return null;
      }

      final uploadUrl = sasData['uploadUrl'] as String;
      final blobName = sasData['blobName'] as String;

      // Step 2: Upload directly to blob storage using SAS URL
      // Determine content type from file extension
      
      final uploadUri = Uri.parse(uploadUrl);
      final uploadResponse = await http.put(
        uploadUri,
        headers: {
          'x-ms-blob-type': 'BlockBlob',
          'Content-Type': mimeType,
        },
        body: fileBytes,
      );

      print('📁 ChatService: Upload to blob status: ${uploadResponse.statusCode}');

      if (uploadResponse.statusCode == 201 || uploadResponse.statusCode == 200) {
        // Step 3: Confirm upload with backend
        final confirmationData = await sendConfirmationUploadRequest(blobName, receiverId);
        if (confirmationData != null) {
          print('✅ ChatService: Document uploaded successfully: $blobName');
          return confirmationData;
        } else {
          print('❌ ChatService: Upload succeeded but confirmation failed');
          return null;
        }
      }

      print('❌ ChatService: Upload to blob failed: ${uploadResponse.statusCode}');
      return null;
    } catch (e) {
      print('❌ ChatService: Failed to upload document to blob: $e');
      return null;
    }
  }

  /// Legacy method for backward compatibility - now uses the new blob upload flow
  @Deprecated('Use uploadDocument instead')
  static Future<String?> uploadImageToBlob(List<int> imageBytes, String fileName) async {
    final result = await uploadDocument(imageBytes, fileName, '');
    return result?['blobName'] as String?;
  }

  /// Legacy method for backward compatibility - now uses the new blob upload flow
  @Deprecated('Use uploadDocument instead')
  static Future<String?> uploadImage(List<int> imageBytes, String fileName) async {
    final result = await uploadDocument(imageBytes, fileName, '');
    return result?['blobName'] as String?;
  }

  /// Get chat history with a specific user via REST API
  static Future<List<Map<String, dynamic>>> getChatHistory(String otherUserId) async {
    try {
      final url = Uri.parse('$baseUrl/chat/history/$otherUserId');
      final response = await ApiService.authenticatedGet(url);

      print('📜 ChatService: Get history status: ${response.statusCode}');

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        return data.map((e) => Map<String, dynamic>.from(e)).toList();
      }

      return [];
    } catch (e) {
      print('❌ ChatService: Failed to get chat history: $e');
      return [];
    }
  }

  /// Get list of all conversations (users we've chatted with)
  /// This endpoint needs to be added to backend, for now we'll use a workaround
  static Future<List<Map<String, dynamic>>> getConversations() async {
    try {
      final url = Uri.parse('$baseUrl/chat/conversations');
      final response = await ApiService.authenticatedGet(url);

      print('📜 ChatService: Get conversations status: ${response.statusCode}');

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        return data.map((e) => Map<String, dynamic>.from(e)).toList();
      }

      // If endpoint doesn't exist yet, return empty list
      return [];
    } catch (e) {
      print('❌ ChatService: Failed to get conversations: $e');
      return [];
    }
  }

  /// Disconnect from SignalR hub
  static Future<void> disconnect() async {
    if (_hubConnection != null) {
      await _hubConnection!.stop();
      _hubConnection = null;
      print('🔌 ChatService: Disconnected from SignalR hub');
    }
    _messageListeners.clear();
  }
}
