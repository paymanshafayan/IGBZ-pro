import 'package:flutter/material.dart';

import 'api/api_client.dart';
import 'screens/dashboard_screen.dart';
import 'screens/login_screen.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const IgBzAdminApp());
}

class IgBzAdminApp extends StatefulWidget {
  const IgBzAdminApp({super.key});

  @override
  State<IgBzAdminApp> createState() => _IgBzAdminAppState();
}

class _IgBzAdminAppState extends State<IgBzAdminApp> {
  final _api = ApiClient();
  bool? _hasToken;

  @override
  void initState() {
    super.initState();
    _checkSession();
  }

  Future<void> _checkSession() async {
    final token = await _api.getToken();
    setState(() => _hasToken = token != null);
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'IGBZ Admin',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
        useMaterial3: true,
      ),
      home: _hasToken == null
          ? const Scaffold(body: Center(child: CircularProgressIndicator()))
          : _hasToken == true
              ? const DashboardScreen()
              : LoginScreen(onLoggedIn: _checkSession),
    );
  }
}
