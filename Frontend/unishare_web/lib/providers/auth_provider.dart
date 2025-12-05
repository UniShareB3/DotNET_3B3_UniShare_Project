import 'package:flutter/foundation.dart';
import '../services/secure_storage_service.dart';
import '../services/api_service.dart';
import '../models/UniversityDto.dart';

class AuthProvider with ChangeNotifier {
  String? _token;
  String? _refreshToken;
  String? _currentUserEmail;
  bool? _emailVerified;

  String? get token => _token;
  String? get refreshToken => _refreshToken;
  String? get currentUserEmail => _currentUserEmail;
  bool? get emailVerified => _emailVerified;

  bool get isAuthenticated => _token != null;

  Map<String, String> _fieldErrors = {};
  Map<String, String> get fieldErrors => _fieldErrors;

  // ----------------- UNIVERSITIES -----------------
  List<UniversityDto> _universities = [];
  List<UniversityDto> get universities => _universities;
  bool _universitiesLoading = false;
  bool get isUniversitiesLoading => _universitiesLoading;

  Future<void> fetchUniversities() async {
    _universitiesLoading = true;
    notifyListeners();
    try {
      final data = await ApiService.getUniversities(); // List<dynamic> de la API
      _universities = data.map((u) => UniversityDto.fromJson(u)).toList();
    } catch (e) {
      _universities = [];
    }
    _universitiesLoading = false;
    notifyListeners();
  }

  // ---------------- LOGIN ----------------
  Future<bool> login(String email, String password) async {
    final response = await ApiService.login(email: email, password: password);

    if (response != null && response.containsKey('accessToken')) {
      _token = response['accessToken'];
      _refreshToken = response['refreshToken'];
      _currentUserEmail = email;

      // decode email verified from token (if present)
      // Prefer server-provided field if available
      try {
        if (response.containsKey('emailVerified')) {
          final ev = response['emailVerified'];
          if (ev is bool) {
            _emailVerified = ev;
          } else if (ev is String) {
            _emailVerified = ev.toLowerCase() == 'true';
          } else {
            _emailVerified = null;
          }
        } else {
          _emailVerified = ApiService.getEmailVerifiedFromToken(_token);
        }
      } catch (e) {
        _emailVerified = null;
      }

      await _saveCredentials();
      notifyListeners();
      return true;
    }
    return false;
  }

  // ---------------- REGISTER ----------------
  Future<Map<String, dynamic>?> register({
    required String firstName,
    required String lastName,
    required String email,
    required String password,
    required String universityName, // now expect universityName
  }) async {
    _fieldErrors.clear();

    final response = await ApiService.register(
      firstName: firstName,
      lastName: lastName,
      email: email,
      password: password,
      universityName: universityName,
    );

    if (response['success'] == false) {
      if (response.containsKey('errors')) {
        _fieldErrors = Map<String, String>.from(response['errors']);
        notifyListeners();
      }
      return null;
    }

    return response; // succes
  }

  String? getUniversityNameById(String id) {
    try {
      return _universities.firstWhere((u) => u.id == id).name;
    } catch (e) {
      return null;
    }
  }

  void clearFieldErrors() {
    _fieldErrors.clear();
    notifyListeners();
  }

  // ---------------- LOGOUT ----------------
  Future<void> logout() async {
    _token = null;
    _refreshToken = null;
    _currentUserEmail = null;
    _emailVerified = null;
    notifyListeners();

    await SecureStorageService.clear();
  }

  // ---------------- AUTO LOGIN ----------------
  Future<void> tryAutoLogin() async {
    final storedToken = await SecureStorageService.getAccessToken();
    final storedRefresh = await SecureStorageService.getRefreshToken();
    final storedEmail = await SecureStorageService.getEmail();

    if (storedToken == null || storedRefresh == null || storedEmail == null) return;

    _token = storedToken;
    _refreshToken = storedRefresh;
    _currentUserEmail = storedEmail;
    // decode email verified from token if available
    try {
      _emailVerified = ApiService.getEmailVerifiedFromToken(_token);
    } catch (e) {
      _emailVerified = null;
    }
    notifyListeners();
  }

  // ---------------- SAVE TOKEN ----------------
  Future<void> _saveCredentials() async {
    if (_token != null && _refreshToken != null && _currentUserEmail != null) {
      await SecureStorageService.saveAccessToken(_token!);
      await SecureStorageService.saveRefreshToken(_refreshToken!);
      await SecureStorageService.saveEmail(_currentUserEmail!);
    }
  }
}
