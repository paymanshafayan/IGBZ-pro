import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../api/api_client.dart';

/// ورود مالک فروشگاه — TenantId + ایمیل + رمز عبور.
class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key, required this.onLoggedIn});

  final VoidCallback onLoggedIn;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _api = ApiClient();
  final _tenantCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  bool _busy = false;
  String? _error;

  Future<void> _login() async {
    final tenant = _tenantCtrl.text.trim();
    final email = _emailCtrl.text.trim();
    final password = _passwordCtrl.text;

    if (tenant.isEmpty || email.isEmpty || password.isEmpty) {
      setState(() => _error = 'شناسهٔ فروشگاه، ایمیل و رمز عبور الزامی است.');
      return;
    }

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      final result = await _api.post('/auth/login', {
        'tenantId': tenant,
        'email': email,
        'password': password,
      });

      final token = result['accessToken'] as String;
      final customerId = result['customerId'] as String;

      await _api.saveSession(token, tenant);
      debugPrint('ورود موفق: $customerId');
      widget.onLoggedIn();
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } catch (e) {
      setState(() => _error = 'خطای شبکه: $e');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('ورود مدیر فروشگاه')),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Icon(Icons.storefront, size: 64, color: Colors.deepPurple),
            const SizedBox(height: 16),
            TextField(
              controller: _tenantCtrl,
              decoration: const InputDecoration(
                labelText: 'شناسهٔ فروشگاه (مثل modstyle)',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _emailCtrl,
              keyboardType: TextInputType.emailAddress,
              decoration: const InputDecoration(
                labelText: 'ایمیل',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _passwordCtrl,
              obscureText: true,
              decoration: const InputDecoration(
                labelText: 'رمز عبور',
                border: OutlineInputBorder(),
              ),
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(_error!, style: const TextStyle(color: Colors.red)),
            ],
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: _busy ? null : _login,
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
              ),
              child: Text(_busy ? 'در حال ورود…' : 'ورود'),
            ),
            const SizedBox(height: 12),
            Text(
              'برای دریافت حساب مالک، ابتدا فروشگاه را از سایت مادر ثبت کنید.',
              style: TextStyle(color: Colors.grey[600], fontSize: 12),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}

// دسترسی به TenantId ذخیره‌شده برای صفحه‌های دیگر
Future<String?> getStoredTenantId() async {
  final prefs = await SharedPreferences.getInstance();
  return prefs.getString('igbz_admin_tenant');
}
