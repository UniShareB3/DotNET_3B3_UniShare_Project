import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:unishare_web/services/secure_storage_service.dart';

import '../main.dart';
import '../models/UniversityDto.dart';

class ApiService {
  static const String baseUrl = 'http://localhost:5083';
  // Debug: store last non-200 response body for received bookings (serialization/server issues)
  static String? lastReceivedError;
  static getUserIdFromToken(String? token){
    final parts = token!.split('.');
    final payload = jsonDecode(utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))));
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
          if (e['code'] == 'DuplicateEmail' || e['code'] == 'DuplicateUserName') {
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
    final url = Uri.parse('$baseUrl/login');

    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email, 'password': password}),
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
            final payload = jsonDecode(utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))));
            final emailVerifiedClaim = payload['email_verified'];

            // Claim might be a bool or a string ('true'/'false') depending on how backend encoded it
            final isVerified = (emailVerifiedClaim is bool && emailVerifiedClaim == true) ||
                (emailVerifiedClaim is String && emailVerifiedClaim.toLowerCase() == 'true');

            // expose verification in returned data so consumers can rely on it
            if (data is Map<String, dynamic>) {
              data['emailVerified'] = isVerified;
            }

            if (!isVerified) {
              ScaffoldMessenger.of(navigatorKey.currentContext!).showSnackBar(
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
      } catch (e) {
        // decoding failed -> silently ignore, don't block login
        print('Failed to decode token for email_verified check: $e');
      }

      return data;
    }

    return null;
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
      final token = await SecureStorageService.getAccessToken(); // citim token-ul

      // Decodare payload pentru ownerId (necesită token valid)
      final ownerId=getUserIdFromToken(token);


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
    final url = Uri.parse('http://localhost:5083/auth/email-verified/${getUserIdFromToken(token)}');
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
  }

  // New: decode the email_verified claim directly from the access token (preferred)
  static bool? getEmailVerifiedFromToken(String? token) {
    if (token == null || token.isEmpty) return null;
    try {
      final parts = token.split('.');
      if (parts.length < 2) return null;
      final payload = jsonDecode(utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))));
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
  static Future<List<Map<String, dynamic>>> getBookingsForItem(String itemId) async {
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
    return await updateBookingStatus(bookingId: bookingId, newStatus: "Approved");
  }

  static Future<bool> rejectBooking(String bookingId) async {
    return await updateBookingStatus(bookingId: bookingId, newStatus: "Rejected");
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
    if (borrowerId == null || borrowerId.toString().isEmpty) {
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

    if (!(startDt.isAfter(DateTime.now().toUtc().subtract(const Duration(minutes: 5))))) {
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

  static Future<List<Map<String, dynamic>>> getReviewsForItem(String itemId) async {
    final allReviews = await getReviews();
    return allReviews.where((r) => r['targetItemId']?.toString() == itemId).toList();
  }

  static Future<Map<String, dynamic>> createReview({
    required String bookingId,
    String? targetUserId,
    String? targetItemId,
    required int rating,
    String? comment,
  }) async {
    final token = await SecureStorageService.getAccessToken();
    if (token == null) return {'success': false, 'status': 401, 'body': 'Not authenticated'};

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
    if (token == null) return {'success': false, 'status': 401, 'body': 'Not authenticated'};

    final reviewerId = getUserIdFromToken(token);
    final url = Uri.parse('$baseUrl/reviews/$reviewId');

    final body = jsonEncode({
      'BookingId': bookingId,
      'ReviewerId': reviewerId,
      'TargetUserId': targetUserId,
      'TargetItemId': targetItemId,
      'Rating': rating,
      'Comment': comment,
      'CreatedAt': DateTime.now().toUtc().toIso8601String(),
    });

    final response = await http.put(
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
    if (token == null) return {'success': false, 'status': 401, 'body': 'Not authenticated'};

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
