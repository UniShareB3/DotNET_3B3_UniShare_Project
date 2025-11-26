import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:unishare_web/services/secure_storage_service.dart';

class ApiService {
  static const String baseUrl = 'http://localhost:5083';

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
      }),
    );

    print('API register status: ${response.statusCode}');
    print('API register body: ${response.body}');

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
      final parts = token!.split('.');
      final payload = jsonDecode(utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))));
      final ownerId = payload['sub']; // sau ce claim folosești pentru ID


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
}