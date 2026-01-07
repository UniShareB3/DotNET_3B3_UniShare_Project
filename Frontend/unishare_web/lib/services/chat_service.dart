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

  /// Send an image message to another user via SignalR
  static Future<bool> sendImageMessage(String receiverId, String imageUrl, {String? caption}) async {
    try {
      final connection = await getConnection();
      if (connection == null) {
        print('❌ ChatService: Cannot send image - not connected');
        return false;
      }

      await connection.invoke('SendImageMessage', args: [receiverId, imageUrl, caption ?? '']);
      print('✅ ChatService: Image sent to $receiverId');
      return true;
    } catch (e) {
      print('❌ ChatService: Failed to send image: $e');
      return false;
    }
  }

  /// Upload an image and return the URL
  static Future<String?> uploadImage(List<int> imageBytes, String fileName) async {
    try {
      final token = await SecureStorageService.getAccessToken();
      if (token == null || token.isEmpty) {
        print('❌ ChatService: No token for image upload');
        return null;
      }

      // Determine content type from file extension
      final extension = fileName.toLowerCase().split('.').last;
      String mimeType;
      switch (extension) {
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
        default:
          print('❌ ChatService: Unsupported file type: $extension');
          return null;
      }

      final url = Uri.parse('$baseUrl/chat/upload-image');
      final request = http.MultipartRequest('POST', url);
      request.headers['Authorization'] = 'Bearer $token';
      request.files.add(http.MultipartFile.fromBytes(
        'image',
        imageBytes,
        filename: fileName,
        contentType: MediaType.parse(mimeType),
      ));

      final streamedResponse = await request.send();
      final response = await http.Response.fromStream(streamedResponse);

      print('📷 ChatService: Upload image status: ${response.statusCode}');

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final imageUrl = data['imageUrl'] as String?;
        if (imageUrl != null) {
          // Return full URL
          return '$baseUrl$imageUrl';
        }
      }

      print('❌ ChatService: Upload failed: ${response.body}');
      return null;
    } catch (e) {
      print('❌ ChatService: Failed to upload image: $e');
      return null;
    }
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

