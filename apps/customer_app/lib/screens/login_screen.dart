import 'package:flutter/material.dart';

import '../api/api_client.dart';

/// ورود/ثبت‌نام مشتری — بعد از موفقیت، نتیجهٔ true برمی‌گرداند.
class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key, this.tenantId});

  final String? tenantId;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _api = ApiClient();
  final _emailCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  bool _busy = false;
  String? _error;

  Future<void> _login() async {
    final email = _emailCtrl.text.trim();
    final password = _passwordCtrl.text;

    if (email.isEmpty || password.isEmpty) {
      setState(() => _error = 'ایمیل و رمز عبور الزامی است.');
      return;
    }

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      final result = await _api.post('/auth/login', {
        'tenantId': widget.tenantId,
        'email': email,
        'password': password,
      }, tenantId: widget.tenantId);

      await _api.saveSession(
        result['accessToken'] as String,
        result['customerId'] as String,
      );

      if (mounted) Navigator.of(context).pop(true);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } catch (e) {
      setState(() => _error = 'خطا: $e');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('ورود')),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextField(
              controller: _emailCtrl,
              keyboardType: TextInputType.emailAddress,
              decoration: const InputDecoration(labelText: 'ایمیل', border: OutlineInputBorder()),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _passwordCtrl,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'رمز عبور', border: OutlineInputBorder()),
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(_error!, style: const TextStyle(color: Colors.red)),
            ],
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: _busy ? null : _login,
              style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 14)),
              child: Text(_busy ? 'در حال ورود…' : 'ورود'),
            ),
          ],
        ),
      ),
    );
  }
}
