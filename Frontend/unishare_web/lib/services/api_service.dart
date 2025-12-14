import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;
import 'package:unishare_web/services/secure_storage_service.dart';

import '../main.dart';

class ApiService {

  // Debug: store last non-200 response body for received bookings (serialization/server issues)
  static String? lastReceivedError;
  
  // IMPORTANT: For Azure deployment, set API_BASE_URL during build:
  // flutter build web --dart-define=API_BASE_URL=https://your-backend-url.azurewebsites.net
  // If not set, defaults to localhost which will cause connection errors in production
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5083', 
  );
  
  // Initialize and log the baseUrl on first access
  static void _logBaseUrl() {
    print('🔧 ApiService initialized with baseUrl: $baseUrl');
    if (baseUrl.contains('localhost') && kIsWeb) {
      print('⚠️  WARNING: Using localhost API URL in web build. Set API_BASE_URL for production!');
    }
  }

  static getUserIdFromToken(String? token) {
    final parts = token!.split('.');
    final payload = jsonDecode(
        utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))));
    final ownerId = payload['sub']; // sau ce claim folosești pentru ID
    return ownerId;
  }

  static Future<List<Map<String, dynamic>>> getMyItems() async {
    final token = await SecureStorageService.getAccessToken();
    final userId = getUserIdFromToken(token);

    final url = Uri.parse('$baseUrl/users/$userId/items');

    final response = await http.get(
      url,
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      if (data is List) return List<Map<String, dynamic>>.from(data);
    }

    return [];
  }

  // ----------------- Get Items -----------------
  static Future<List<Map<String, dynamic>>> getItems() async {
    final token = await SecureStorageService.getAccessToken();
    final url = Uri.parse('$baseUrl/items');

    final response = await http.get(
      url,
      headers: {
        'Content-Type': 'application/json',
        if (token != null && token.isNotEmpty) 'Authorization': 'Bearer $token',
      },
    );

    print('API get-items status: ${response.statusCode}');
    print('API get-items body: ${response.body}');

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      if (data is List) return List<Map<String, dynamic>>.from(data);
    }

    return [];
  }

  // ----------------- Confirm Email -----------------
  static Future<bool> confirmEmail(String userId, String code) async {
    final url = Uri.parse('$baseUrl/auth/confirm-email');

    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'userId': userId, 'code': code}),
    );

    print('API confirm-email status: ${response.statusCode}');
    print('API confirm-email body: ${response.body}');

    return response.statusCode == 200;
  }

  // ----------------- Password Reset (Request / Verify / Change) -----------------
  // Request a password reset (backend expects Email and optionally frontendUrl)
  static Future<bool> requestPasswordResetByEmail(String email) async {
    final url = Uri.parse('$baseUrl/auth/password-reset/request');
    // Get the current frontend origin (e.g., http://localhost:59057)
    final frontendUrl = Uri.base.origin;

    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email, 'frontendUrl': frontendUrl}),
    );

    print('API password-reset-request status: ${response.statusCode}');
    print('API password-reset-request body: ${response.body}');

    return response.statusCode == 200 || response.statusCode == 204;
  }

  // Verify password reset token (from email link). Returns temporaryToken if verified.
  static Future<String?> verifyPasswordReset(String userId, String code) async {
    // Ensure code is URL-decoded then re-encoded for safe transmission
    final decoded = Uri.decodeComponent(code);
    final encoded = Uri.encodeComponent(decoded);
    final uri = Uri.parse(
        '$baseUrl/auth/password?userId=$userId&code=$encoded');

    final response = await http.get(
        uri, headers: {'Content-Type': 'application/json'});

    print('API verify-password status: ${response.statusCode}');
    print('API verify-password body: ${response.body}');

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      // backend returns { temporaryToken: "...", expiresInMinutes: 5 }
      if (data is Map && data['temporaryToken'] != null) {
        return data['temporaryToken'] as String;
      }
      return null;
    }

    return null;
  }

  // Change password using the temporary token returned by verifyPasswordReset
  // Returns a map with 'success' boolean and optional 'errors' map
  static Future<Map<String, dynamic>> changePasswordWithTempToken({
    required String userId,
    required String newPassword,
    required String temporaryToken,
  }) async {
    final url = Uri.parse('$baseUrl/users/password');

    final response = await http.post(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $temporaryToken',
      },
      body: jsonEncode({'newPassword': newPassword, 'userId': userId}),
    );

    print('API change-password status: ${response.statusCode}');
    print('API change-password body: ${response.body}');

    if (response.statusCode == 200 || response.statusCode == 204) {
      return {'success': true};
    } else if (response.statusCode == 400) {
      // Parse validation errors from response
      try {
        final Map<String, dynamic> errorData = jsonDecode(response.body);
        return {
          'success': false,
          'errors': errorData,
        };
      } catch (e) {
        return {
          'success': false,
          'errors': {'general': ['Failed to change password']},
        };
      }
    } else {
      return {
        'success': false,
        'errors': {'general': ['An unexpected error occurred']},
      };
    }
  }

  // ----------------- Get User -----------------
  static Future<Map<String, dynamic>?> getUser(String userId) async {
    final token = await SecureStorageService.getAccessToken();

    if (token == null) {
      print('API get-user: No token available');
      return null;
    }

    final url = Uri.parse('$baseUrl/users/$userId');

    final response = await http.get(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
    );

    print('API get-user status: ${response.statusCode}');
    print('API get-user body: ${response.body}');

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    } else if (response.statusCode == 401) {
      print('API get-user: Unauthorized - token may be expired');
    }
    return null;
  }

  // ----------------- Update User -----------------
  static Future<Map<String, dynamic>> updateUser({
    required String userId,
    String? firstName,
    String? lastName,
    String? email,
    String? universityName,
  }) async {
    final token = await SecureStorageService.getAccessToken();

    if (token == null) {
      print('API update-user: No token available');
      return {
        'success': false,
        'errors': {'general': ['Session expired. Please log in again.']},
      };
    }

    final url = Uri.parse('$baseUrl/users/$userId');

    final payload = <String, dynamic>{};
    if (firstName != null) payload['firstName'] = firstName;
    if (lastName != null) payload['lastName'] = lastName;
    if (email != null) payload['email'] = email;
    if (universityName != null) payload['universityName'] = universityName;

    final response = await http.patch(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode(payload),
    );

    print('API update-user status: ${response.statusCode}');
    print('API update-user body: ${response.body}');

    if (response.statusCode == 200 || response.statusCode == 204) {
      return {'success': true};
    } else if (response.statusCode == 400) {
      // Parse validation errors from response
      try {
        final dynamic errorData = jsonDecode(response.body);

        // Backend can return either:
        // 1. Array format: [{"code":"InvalidEmailDomain","description":"..."}]
        // 2. Object format: {"Email": ["error message"], "FirstName": [...]}

        if (errorData is List) {
          // Array format - convert to our expected format
          final Map<String, List<String>> parsedErrors = {};

          for (var error in errorData) {
            if (error is Map<String, dynamic>) {
              final code = error['code'] as String?;
              final description = error['description'] as String?;

              if (description != null) {
                // Try to map error code to field name
                String fieldKey = 'general';
                if (code != null) {
                  if (code.toLowerCase().contains('email')) {
                    fieldKey = 'Email';
                  } else if (code.toLowerCase().contains('firstname')) {
                    fieldKey = 'FirstName';
                  } else if (code.toLowerCase().contains('lastname')) {
                    fieldKey = 'LastName';
                  } else if (code.toLowerCase().contains('university')) {
                    fieldKey = 'University';
                  }
                }

                parsedErrors[fieldKey] = parsedErrors[fieldKey] ?? [];
                parsedErrors[fieldKey]!.add(description);
              }
            }
          }

          return {
            'success': false,
            'errors': parsedErrors,
          };
        } else if (errorData is Map<String, dynamic>) {
          // Object format - return as is
          return {
            'success': false,
            'errors': errorData,
          };
        }

        // Fallback if format is unexpected
        return {
          'success': false,
          'errors': {'general': ['Failed to update profile']},
        };
      } catch (e) {
        return {
          'success': false,
          'errors': {'general': ['Failed to update profile']},
        };
      }
    } else {
      return {
        'success': false,
        'errors': {'general': ['An unexpected error occurred']},
      };
    }
  }

  // ----------------- Register -----------------
  static Future<Map<String, dynamic>> register({
    required String firstName,
    required String lastName,
    required String email,
    required String password,
    required String universityName, // changed to send name (backend expects UniversityName)
    String? universityId, // optional: include id for backwards compatibility
  }) async {
    final url = Uri.parse('$baseUrl/register');

    final payload = {
      'firstName': firstName,
      'lastName': lastName,
      'email': email,
      'password': password,
      'universityName': universityName,
    };

    if (universityId != null) {
      payload['universityId'] = universityId;
    }

    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode(payload),
    );

    if (response.statusCode >= 200 && response.statusCode < 300) {
      var rep = json.decode(response.body);
      rep['success'] = true;
      return rep;
    } else {
      final data = jsonDecode(response.body);
      Map<String, String> errors = {};
      if (data is List) {
        for (var e in data) {
          if (e['code'] == 'DuplicateEmail' ||
              e['code'] == 'DuplicateUserName') {
            errors['email'] = e['description'];
          }
        }
      }
      return {'success': false, 'errors': errors};
    }
  }

  // ----------------- Login -----------------
  static Future<Map<String, dynamic>?> login({
    required String email,
    required String password,
  }) async {
    _logBaseUrl(); // Log baseUrl configuration
    try {
      final url = Uri.parse('$baseUrl/login');

      final response = await http.post(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'email': email, 'password': password}),
      ).timeout(
        const Duration(seconds: 30),
        onTimeout: () {
          throw Exception('Request timeout - unable to connect to server');
        },
      );

      print('API login status: ${response.statusCode}');
      print('API login body: ${response.body}');

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);

        await SecureStorageService.saveAccessToken(data['accessToken']);
        await SecureStorageService.saveRefreshToken(data['refreshToken']);
        await SecureStorageService.saveEmail(email);

        // Check email verification using the JWT's 'email_verified' claim
        try {
          final token = data['accessToken'] as String?;
          if (token != null && token.isNotEmpty) {
            final parts = token.split('.');
            if (parts.length >= 2) {
              final payload = jsonDecode(
                  utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))));
              final emailVerifiedClaim = payload['email_verified'];

              // Claim might be a bool or a string ('true'/'false') depending on how backend encoded it
              final isVerified = (emailVerifiedClaim is bool &&
                  emailVerifiedClaim == true) ||
                  (emailVerifiedClaim is String &&
                      emailVerifiedClaim.toLowerCase() == 'true');

              // expose verification in returned data so consumers can rely on it
              if (data is Map<String, dynamic>) {
                data['emailVerified'] = isVerified;
              }

              if (!isVerified) {
                final context = navigatorKey.currentContext;
                if (context != null) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text(
                        'Email not verified. Please go to your profile to verify your email.',
                      ),
                      backgroundColor: Colors.orange,
                    ),
                  );
                }
              }
            }
          }
        } catch (e) {
          // decoding failed -> silently ignore, don't block login
          print('Failed to decode token for email_verified check: $e');
        }

        return data;
      } else if (response.statusCode == 401) {
        // Backend returns 401 with empty body for invalid password
        return {'error': 'Invalid password or email.'};
      } else {
        // Parse error message for other invalid credentials
        try {
          final body = jsonDecode(response.body);
          return {'error': body['detail'] ?? 'Invalid email or password'};
        } catch (e) {
          return {'error': 'Server error: ${response.statusCode}'};
        }
      }
    } catch (e) {
      print('API login error: $e');
      // Handle network errors, CORS errors, etc.
      String errorMessage = 'Unable to connect to server';
      if (e.toString().contains('CORS') || e.toString().contains('Failed host lookup')) {
        errorMessage = 'Network error: Please check your connection and API configuration';
      } else if (e.toString().contains('timeout')) {
        errorMessage = 'Connection timeout: Server is not responding';
      } else if (baseUrl.contains('localhost')) {
        errorMessage = 'Configuration error: API URL is set to localhost. Please configure API_BASE_URL for production.';
      }
      return {'error': errorMessage};
    }
  }

  // ----------------- Post Item -----------------
  static Future<bool> postItem({
    required String name,
    required String description,
    required String category,
    required String condition,
    String? imageUrl, // NOU: Acceptă URL-ul imaginii
  }) async {
    try {
      final token = await SecureStorageService
          .getAccessToken(); // citim token-ul

      // Decodare payload pentru ownerId (necesită token valid)
      final ownerId = getUserIdFromToken(token);


      final url = Uri.parse('$baseUrl/items');
      final headers = {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      };
      final body = jsonEncode({
        'item': {
          'name': name,
          'description': description,
          'category': category,
          'condition': condition,
          'ownerId': ownerId,
          "imageUrl": imageUrl // NOU: Trimite URL-ul
        }
      });

      print('Posting item to $url');
      print('Headers: $headers');
      print('Body: $body');

      final response = await http.post(url, headers: headers, body: body);

      print('Response status: ${response.statusCode}');
      print('Response body: ${response.body}');

      if (response.statusCode == 201 || response.statusCode == 200) {
        return true;
      } else {
        print('Failed to create item');
        return false;
      }
    } catch (e) {
      print('Exception during postItem: $e');
      return false;
    }
  }

  static Future<bool?> getEmailVerifiedStatus(String token) async {
    try {
      final url = Uri.parse(
          '$baseUrl/auth/email-verified/${getUserIdFromToken(token)}');
      final response = await http.get(
        url,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        return data['emailVerified'] as bool?;
      }

      return null;
    } catch (e) {
      print('getEmailVerifiedStatus error: $e');
      return null;
    }
  }

  // New: decode the email_verified claim directly from the access token (preferred)
  static bool? getEmailVerifiedFromToken(String? token) {
    if (token == null || token.isEmpty) return null;
    try {
      final parts = token.split('.');
      if (parts.length < 2) return null;
      final payload = jsonDecode(
          utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))));
      final claim = payload['email_verified'];
      if (claim == null) return null;
      if (claim is bool) return claim;
      if (claim is String) return claim.toLowerCase() == 'true';
    } catch (e) {
      print('Failed to decode email_verified from token: $e');
    }
    return null;
  }

// ----------------- Get Bookings For Item -----------------
  static Future<List<Map<String, dynamic>>> getBookingsForItem(
      String itemId) async {
    final token = await SecureStorageService.getAccessToken();
    final url = Uri.parse('$baseUrl/items/$itemId/bookings');

    final response = await http.get(
      url,
      headers: {
        'Content-Type': 'application/json',
        if (token != null && token.isNotEmpty) 'Authorization': 'Bearer $token',
      },
    );

    print('API get-item-bookings status: ${response.statusCode}');
    print('API get-item-bookings body: ${response.body}');

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      if (data is List) return List<Map<String, dynamic>>.from(data);
    }

    return [];
  }


// ----------------- Filter Bookings Sent (Requests Sent) -----------------
  static Future<List<Map<String, dynamic>>> getMyBookings() async {
    final token = await SecureStorageService.getAccessToken();
    if (token == null) return [];

    final userId = getUserIdFromToken(token);
    final url = Uri.parse('$baseUrl/users/$userId/bookings');

    final response = await http.get(url, headers: {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    });

    print('API get-user-bookings status: ${response.statusCode}');
    print('API get-user-bookings body: ${response.body}');

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      if (data is List) return List<Map<String, dynamic>>.from(data);
    }

    return [];
  }


// ----------------- Filter Bookings Received (Requests Received) -----------------
  static Future<List<Map<String, dynamic>>> getReceivedBookings() async {
    final token = await SecureStorageService.getAccessToken();
    if (token == null) return [];

    final userId = getUserIdFromToken(token);
    final url = Uri.parse('$baseUrl/users/$userId/booked-items');

    final response = await http.get(url, headers: {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    });

    print('API get-user-booked-items status: ${response.statusCode}');
    print('API get-user-booked-items body: ${response.body}');

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      if (data is List) return List<Map<String, dynamic>>.from(data);
    } else {
      // capture error body so UI can show explanation
      lastReceivedError = response.body;
    }

    return [];
  }

  // ----------------- Get Single Item -----------------
  static Future<Map<String, dynamic>> getItemById(String itemId) async {
    final token = await SecureStorageService.getAccessToken();
    final url = Uri.parse('$baseUrl/items/$itemId');

    final response = await http.get(url, headers: {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    });

    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    }
    return {};
  }

// ----------------- Get Single User -----------------
  static Future<Map<String, dynamic>> getUserById(String userId) async {
    final token = await SecureStorageService.getAccessToken();
    final url = Uri.parse('$baseUrl/users/$userId');

    final response = await http.get(url, headers: {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    });

    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    }
    return {};
  }

  static Future<bool> updateBookingStatus({
    required String bookingId,
    required String newStatus, // "Approved" / "Rejected"
  }) async {
    final token = await SecureStorageService.getAccessToken();
    if (token == null) return false;

    final url = Uri.parse('$baseUrl/bookings/$bookingId/status');
    final response = await http.put(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode({'status': newStatus}),
    );

    print('Update booking $bookingId to $newStatus -> ${response.statusCode}');
    if (response.statusCode == 200) {
      return true;
    } else {
      print('Failed: ${response.body}');
      return false;
    }
  }

  // Shortcut-uri
  static Future<bool> approveBooking(String bookingId) async {
    return await updateBookingStatus(
        bookingId: bookingId, newStatus: "Approved");
  }

  static Future<bool> rejectBooking(String bookingId) async {
    return await updateBookingStatus(
        bookingId: bookingId, newStatus: "Rejected");
  }

  static Future<List<dynamic>> getUniversities() async {
    final response = await http.get(Uri.parse('$baseUrl/universities'));

    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    }

    return [];
  }

  static Future<bool> sendVerificationCode(String userId) async {
    final url = Uri.parse('$baseUrl/auth/send-verification-code');
    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'userId': userId}),
    );

    if (response.statusCode == 200) {
      return true;
    } else {
      print('Failed to send verification code: ${response.body}');
      return false;
    }
  }

  static Future<bool> createBooking({
    required String itemId,
    required String startDateIso,
    required String endDateIso,
  }) async {
    final token = await SecureStorageService.getAccessToken();
    if (token == null) return false;

    final borrowerId = getUserIdFromToken(token);
    // Client-side validation before sending to backend
    if (itemId.isEmpty) {
      print('createBooking: missing itemId');
      return false;
    }
    if (borrowerId == null || borrowerId
        .toString()
        .isEmpty) {
      print('createBooking: missing borrowerId (token issue)');
      return false;
    }

    DateTime? startDt;
    DateTime? endDt;
    try {
      startDt = DateTime.parse(startDateIso).toUtc();
      endDt = DateTime.parse(endDateIso).toUtc();
    } catch (e) {
      print('createBooking: invalid date format: $e');
      return false;
    }

    if (!startDt.isBefore(endDt)) {
      print('createBooking: StartDate must be before EndDate');
      return false;
    }

    if (!(startDt.isAfter(
        DateTime.now().toUtc().subtract(const Duration(minutes: 5))))) {
      print('createBooking: StartDate is in the past (or too close to now)');
      return false;
    }
    final url = Uri.parse('$baseUrl/bookings');

    final body = jsonEncode({
      'ItemId': itemId,
      'BorrowerId': borrowerId,
      'RequestedOn': DateTime.now().toUtc().toIso8601String(),
      'StartDate': startDateIso,
      'EndDate': endDateIso,
    });

    final response = await http.post(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: body,
    );

    print('Create booking status: ${response.statusCode}');
    print('Create booking body: ${response.body}');

    return response.statusCode == 201 || response.statusCode == 200;
  }

  // ----------------- Reviews -----------------
  static Future<List<Map<String, dynamic>>> getReviews() async {
    final url = Uri.parse('$baseUrl/reviews');

    final response = await http.get(url, headers: {
      'Content-Type': 'application/json',
    });

    print('API get-reviews status: ${response.statusCode}');
    print('API get-reviews body: ${response.body}');

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      if (data is List) return List<Map<String, dynamic>>.from(data);
    }

    return [];
  }

  static Future<List<Map<String, dynamic>>> getReviewsForItem(
      String itemId) async {
    final allReviews = await getReviews();
    return allReviews
        .where((r) => r['targetItemId']?.toString() == itemId)
        .toList();
  }

  static Future<Map<String, dynamic>> createReview({
    required String bookingId,
    String? targetUserId,
    String? targetItemId,
    required int rating,
    String? comment,
  }) async {
    final token = await SecureStorageService.getAccessToken();
    if (token == null)
      return {'success': false, 'status': 401, 'body': 'Not authenticated'};

    final reviewerId = getUserIdFromToken(token);
    final url = Uri.parse('$baseUrl/reviews');

    final body = jsonEncode({
      'BookingId': bookingId,
      'ReviewerId': reviewerId,
      'TargetUserId': targetUserId,
      'TargetItemId': targetItemId,
      'Rating': rating,
      'Comment': comment,
      'CreatedAt': DateTime.now().toUtc().toIso8601String(),
    });

    final response = await http.post(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: body,
    );

    print('🔍 [CREATE REVIEW] Status: ${response.statusCode}');
    print('🔍 [CREATE REVIEW] Body length: ${response.body.length}');
    print('🔍 [CREATE REVIEW] Body: ${response.body}');
    print('🔍 [CREATE REVIEW] Headers: ${response.headers}');

    return {
      'success': response.statusCode == 201 || response.statusCode == 200,
      'status': response.statusCode,
      'body': response.body,
    };
  }

  static Future<Map<String, dynamic>> updateReview({
    required String reviewId,
    required String bookingId,
    String? targetUserId,
    String? targetItemId,
    required int rating,
    String? comment,
  }) async {
    final token = await SecureStorageService.getAccessToken();
    if (token == null)
      return {'success': false, 'status': 401, 'body': 'Not authenticated'};

    final reviewerId = getUserIdFromToken(token);
    final url = Uri.parse('$baseUrl/reviews/$reviewId');

    // Send only the fields required by UpdateReviewDto on the backend (Rating and Comment).
    final body = jsonEncode({
      'rating': rating,
      'comment': comment,
    });

    final response = await http.patch(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: body,
    );

    print('🔍 [UPDATE REVIEW] Status: ${response.statusCode}');
    print('🔍 [UPDATE REVIEW] Body: ${response.body}');

    return {
      'success': response.statusCode == 200,
      'status': response.statusCode,
      'body': response.body,
    };
  }

  static Future<Map<String, dynamic>> deleteReview({
    required String reviewId,
  }) async {
    final token = await SecureStorageService.getAccessToken();
    if (token == null)
      return {'success': false, 'status': 401, 'body': 'Not authenticated'};

    final url = Uri.parse('$baseUrl/reviews/$reviewId');
    final response = await http.delete(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
    );
    print('🔍 [DELETE REVIEW] Status: ${response.statusCode}');
    print('🔍 [DELETE REVIEW] Body: ${response.body}');
    return {
      'success': response.statusCode == 200,
      'status': response.statusCode,
      'body': response.body,
    };
  }
}
