import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'secure_storage_service.dart';

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
  /// This expects the blobPath (path in blob storage)
  static Future<bool> sendDocumentMessage(String receiverId, String blobPath, {String? caption}) async {
    try {
      final connection = await getConnection();
      if (connection == null) {
        print('❌ ChatService: Cannot send document - not connected');
        return false;
      }

      await connection.invoke('SendImageMessage', args: [receiverId, blobPath, caption ?? '']);
      print('✅ ChatService: Document sent to $receiverId');
      return true;
    } catch (e) {
      print('❌ ChatService: Failed to send document: $e');
      return false;
    }
  }

  /// Legacy method for backward compatibility
  @Deprecated('Use sendDocumentMessage instead')
  static Future<bool> sendImageMessage(String receiverId, String blobPath, {String? caption}) async {
    return sendDocumentMessage(receiverId, blobPath, caption: caption);
  }

  /// Retrieve SAS URL for uploading a document to blob storage
  static Future<Map<String, dynamic>?> retrieveSasUrl(String fileName) async {
    try {
      final token = await SecureStorageService.getAccessToken();
      if (token == null || token.isEmpty) {
        print('❌ ChatService: No token for retrieving SAS URL');
        return null;
      }

      final url = Uri.parse('$baseUrl/documents/upload-url');
      final response = await http.post(
        url,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode({
          'fileName': fileName,
        }),
      );

      print('🔗 ChatService: Get SAS URL status: ${response.statusCode}');

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        // Expected response: { "sasUrl": "...", "blobPath": "..." }
        return {
          'sasUrl': data['sasUrl'] as String,
          'blobPath': data['blobPath'] as String,
        };
      }

      print('❌ ChatService: Failed to get SAS URL: ${response.body}');
      return null;
    } catch (e) {
      print('❌ ChatService: Failed to retrieve SAS URL: $e');
      return null;
    }
  }

  /// Send confirmation to backend after successful upload to blob storage
  static Future<bool> sendConfirmationUploadRequest(String blobPath) async {
    try {
      final token = await SecureStorageService.getAccessToken();
      if (token == null || token.isEmpty) {
        print('❌ ChatService: No token for confirming upload');
        return false;
      }

      final url = Uri.parse('$baseUrl/documents/confirm-upload');
      final response = await http.post(
        url,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode({
          'blobPath': blobPath,
        }),
      );

      print('✅ ChatService: Confirm upload status: ${response.statusCode}');

      if (response.statusCode == 200 || response.statusCode == 204) {
        print('✅ ChatService: Upload confirmed successfully');
        return true;
      }

      print('❌ ChatService: Failed to confirm upload: ${response.body}');
      return false;
    } catch (e) {
      print('❌ ChatService: Failed to send confirmation: $e');
      return false;
    }
  }

  /// Upload a document directly to blob storage using SAS URL
  /// Returns the blobPath on success, null on failure
  /// Supports images and other file types
  static Future<String?> uploadDocument(List<int> fileBytes, String fileName) async {
    try {
      // Step 1: Retrieve SAS URL and blob path
      final sasData = await retrieveSasUrl(fileName);
      if (sasData == null) {
        print('❌ ChatService: Failed to retrieve SAS URL');
        return null;
      }

      final sasUrl = sasData['sasUrl'] as String;
      final blobPath = sasData['blobPath'] as String;

      // Step 2: Upload directly to blob storage using SAS URL
      // Determine content type from file extension
      final extension = fileName.toLowerCase().split('.').last;
      String mimeType;
      switch (extension) {
        // Image types
        case 'jpg':
        case 'jpeg':
          mimeType = 'image/jpeg';
          break;
        case 'png':
          mimeType = 'image/png';
          break;
        case 'gif':
          mimeType = 'image/gif';
          break;
        case 'webp':
          mimeType = 'image/webp';
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

      final uploadUrl = Uri.parse(sasUrl);
      final uploadResponse = await http.put(
        uploadUrl,
        headers: {
          'x-ms-blob-type': 'BlockBlob',
          'Content-Type': mimeType,
        },
        body: fileBytes,
      );

      print('📁 ChatService: Upload to blob status: ${uploadResponse.statusCode}');

      if (uploadResponse.statusCode == 201 || uploadResponse.statusCode == 200) {
        // Step 3: Confirm upload with backend
        final confirmed = await sendConfirmationUploadRequest(blobPath);
        if (confirmed) {
          print('✅ ChatService: Document uploaded successfully: $blobPath');
          return blobPath;
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
    return uploadDocument(imageBytes, fileName);
  }

  /// Legacy method for backward compatibility - now uses the new blob upload flow
  @Deprecated('Use uploadDocument instead')
  static Future<String?> uploadImage(List<int> imageBytes, String fileName) async {
    return uploadDocument(imageBytes, fileName);
  }

  /// Get chat history with a specific user via REST API
  static Future<List<Map<String, dynamic>>> getChatHistory(String otherUserId) async {
    try {
      final token = await SecureStorageService.getAccessToken();
      if (token == null || token.isEmpty) {
        print('❌ ChatService: No token for chat history');
        return [];
      }

      final url = Uri.parse('$baseUrl/chat/history/$otherUserId');
      final response = await http.get(
        url,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

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
      final token = await SecureStorageService.getAccessToken();
      if (token == null || token.isEmpty) {
        print('❌ ChatService: No token for conversations');
        return [];
      }

      final url = Uri.parse('$baseUrl/chat/conversations');
      final response = await http.get(
        url,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

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
