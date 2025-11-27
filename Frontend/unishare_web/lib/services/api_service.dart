import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:unishare_web/services/secure_storage_service.dart';

import '../main.dart';
import '../models/UniversityDto.dart';

class ApiService {
  static const String baseUrl = 'http://localhost:5083';
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
    required String universityId, // nou
  }) async {
    final url = Uri.parse('$baseUrl/register');

    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'firstName': firstName,
        'lastName': lastName,
        'email': email,
        'password': password,
        'universityId': universityId, // adăugat
      }),
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

      // ❗ Verificăm dacă email-ul nu e verificat și afișăm SnackBar
      if (data['emailVerified'] != null && data['emailVerified'] == false) {
        // Trebuie să ai un navigatorKey definit în MaterialApp pentru a putea folosi context aici
        ScaffoldMessenger.of(navigatorKey.currentContext!).showSnackBar(
          const SnackBar(
            content: Text(
              'Email not verified. Please go to your profile to verify your email.',
            ),
            backgroundColor: Colors.orange,
          ),
        );
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
  // ----------------- Get All Bookings -----------------
  static Future<List<Map<String, dynamic>>> getBookings() async {
    final token = await SecureStorageService.getAccessToken();
    final url = Uri.parse('$baseUrl/bookings');

    final response = await http.get(
      url,
      headers: {
        'Content-Type': 'application/json',
        if (token != null && token.isNotEmpty) 'Authorization': 'Bearer $token',
      },
    );

    print('API get-bookings status: ${response.statusCode}');
    print('API get-bookings body: ${response.body}');

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

    // Preluăm toate booking-urile
    final allBookings = await getBookings();

    // Filtrăm doar ce a trimis userul curent (borrowerId)
    final sent = allBookings.where((b) => b['borrowerId'] == userId).toList();

    print("Filtered MyBookings (sent): ${sent.length}");

    return sent;
  }



// ----------------- Filter Bookings Received (Requests Received) -----------------
  static Future<List<Map<String, dynamic>>> getReceivedBookings() async {
    final token = await SecureStorageService.getAccessToken();
    if (token == null) return [];

    final userId = getUserIdFromToken(token);

    // 1. Preluăm itemele userului curent
    final myItems = await getMyItems();
    final myItemIds = myItems.map((i) => i['id']).toList();

    // Debug
    print("My item IDs: $myItemIds");

    if (myItemIds.isEmpty) {
      print("No items found for current user.");
      return [];
    }

    // 2. Preluăm toate booking-urile
    final allBookings = await getBookings();

    // 3. Filtrăm booking-urile pentru itemele mele
    final received = allBookings.where((b) => myItemIds.contains(b['itemId'])).toList();

    print("Filtered ReceivedBookings: ${received.length}");

    return received;
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



}