import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

/// کلاینت API اپ مشتری — توکن JWT مشتری در SharedPreferences.
class ApiClient {
  ApiClient({this.baseUrl = 'http://10.0.2.2:5000/api'});

  final String baseUrl;

  static const _tokenKey = 'igbz_customer_token';
  static const _customerIdKey = 'igbz_customer_id';

  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_tokenKey);
  }

  Future<String?> getCustomerId() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_customerIdKey);
  }

  Future<void> saveSession(String token, String customerId) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_tokenKey, token);
    await prefs.setString(_customerIdKey, customerId);
  }

  Future<void> clearSession() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokenKey);
    await prefs.remove(_customerIdKey);
  }

  Future<Map<String, dynamic>> _request(
    String method,
    String path, {
    Map<String, dynamic>? body,
    String? tenantId,
  }) async {
    final token = await getToken();
    final uri = Uri.parse('$baseUrl$path');

    final request = http.Request(method, uri);
    request.headers['Content-Type'] = 'application/json';
    if (tenantId != null) {
      request.headers['X-Tenant-Id'] = tenantId;
    }
    if (token != null) {
      request.headers['Authorization'] = 'Bearer $token';
    }
    if (body != null) {
      request.body = jsonEncode(body);
    }

    final streamed = await request.send();
    final response = await http.Response.fromStream(streamed);

    final decoded = response.body.isEmpty
        ? <String, dynamic>{}
        : jsonDecode(response.body) as Map<String, dynamic>;

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(
        decoded['message']?.toString() ?? 'خطا ${response.statusCode}',
        response.statusCode,
      );
    }

    return decoded;
  }

  Future<Map<String, dynamic>> get(String path, {String? tenantId}) =>
      _request('GET', path, tenantId: tenantId);
  Future<Map<String, dynamic>> post(String path, Map<String, dynamic> body, {String? tenantId}) =>
      _request('POST', path, body: body, tenantId: tenantId);
}

class ApiException implements Exception {
  ApiException(this.message, this.statusCode);

  final String message;
  final int statusCode;

  @override
  String toString() => message;
}
