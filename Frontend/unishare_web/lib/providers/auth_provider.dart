import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../services/api_service.dart';

class AuthProvider with ChangeNotifier {
  String? _token;
  String? _refreshToken;
  String? _currentUserEmail;

  String? get token => _token;
  String? get refreshToken => _refreshToken;
  String? get currentUserEmail => _currentUserEmail;

  bool get isAuthenticated => _token != null;

  Map<String, String> _fieldErrors = {};
  Map<String, String> get fieldErrors => _fieldErrors;

  // ---------------- LOGIN ----------------
  Future<bool> login(String email, String password) async {
    print('🔑 Login attempt for $email');

    final response = await ApiService.login(email: email, password: password);

    if (response != null && response.containsKey('accessToken')) {
      _token = response['accessToken'];
      _refreshToken = response['refreshToken'];
      _currentUserEmail = email;

      await _saveCredentials();
      notifyListeners();

      print('✅ Login successful, token saved');
      return true;
    }

    print('❌ Login failed');
    return false;
  }

  // ---------------- REGISTER ----------------
  Future<bool> register({
    required String firstName,
    required String lastName,
    required String email,
    required String userName,
    required String password,
  }) async {
    print('📝 Register request: $firstName, $lastName, $email, $userName');

    final result = await ApiService.register(
      firstName: firstName,
      lastName: lastName,
      email: email,
      userName: userName,
      password: password,
    );

    print('📩 Register response: $result');

    if (result['success'] == true) {
      _fieldErrors.clear();
      return true;
    } else {
      _fieldErrors = result['errors'] ?? {};
      return false;
    }
  }

  void clearFieldErrors() {
    _fieldErrors.clear();
    notifyListeners();
  }

  // ---------------- LOGOUT ----------------
  Future<void> logout() async {
    print('🚪 Logging out user $_currentUserEmail');
    _token = null;
    _refreshToken = null;
    _currentUserEmail = null;
    notifyListeners();

    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('token');
    await prefs.remove('refreshToken');
    await prefs.remove('userEmail');
  }

  // ---------------- AUTO LOGIN ----------------
  Future<void> tryAutoLogin() async {
    final prefs = await SharedPreferences.getInstance();
    if (!prefs.containsKey('token')) return;

    _token = prefs.getString('token');
    _refreshToken = prefs.getString('refreshToken');
    _currentUserEmail = prefs.getString('userEmail');

    notifyListeners();
    print('♻️ Auto-login for $_currentUserEmail');
  }

  // ---------------- SAVE TOKEN ----------------
  Future<void> _saveCredentials() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('token', _token ?? '');
    await prefs.setString('refreshToken', _refreshToken ?? '');
    await prefs.setString('userEmail', _currentUserEmail ?? '');
  }
}
