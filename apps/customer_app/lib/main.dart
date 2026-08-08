import 'dart:async';

import 'package:app_links/app_links.dart';
import 'package:flutter/material.dart';

import 'models/cart.dart';
import 'screens/cart_screen.dart';
import 'screens/home_screen.dart';
import 'screens/product_screen.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Cart.instance.load();
  runApp(const IgBzCustomerApp());
}

class IgBzCustomerApp extends StatefulWidget {
  const IgBzCustomerApp({super.key});

  @override
  State<IgBzCustomerApp> createState() => _IgBzCustomerAppState();
}

class _IgBzCustomerAppState extends State<IgBzCustomerApp> {
  final _navigatorKey = GlobalKey<NavigatorState>();

  // Deep Link: استخراج تننت/محصول از لینک (فاز 5)
  // نمونه لینک: igbz://store/modstyle یا igbz://product/{slug}?tenant={tenantId}
  StreamSubscription<Uri>? _linkSub;
  String? _tenantId;

  @override
  void initState() {
    super.initState();
    _initDeepLinks();
  }

  Future<void> _initDeepLinks() async {
    final appLinks = AppLinks();

    _linkSub = appLinks.uriLinkStream.listen(_handleDeepLink);

    // لینک اولیه (اگر اپ با لینک باز شده باشد)
    final initial = await appLinks.getInitialLink();
    if (initial != null) {
      _handleDeepLink(initial);
    }
  }

  void _handleDeepLink(Uri uri) {
    // igbz://store/{tenant}
    // igbz://product/{slug}?tenant={tenant}
    final segments = uri.pathSegments;
    final tenant = uri.queryParameters['tenant'];

    if (segments.isNotEmpty && segments.first == 'store' && segments.length >= 2) {
      _navigatorKey.currentState?.push(
        MaterialPageRoute(builder: (_) => HomeScreen(tenantId: segments[1])),
      );
      return;
    }

    if (segments.isNotEmpty && segments.first == 'product' && segments.length >= 2) {
      _navigatorKey.currentState?.push(
        MaterialPageRoute(
          builder: (_) => ProductScreen(slug: segments[1], tenantId: tenant),
        ),
      );
    }
  }

  @override
  void dispose() {
    _linkSub?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'IGBZ',
      debugShowCheckedModeBanner: false,
      navigatorKey: _navigatorKey,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
        useMaterial3: true,
      ),
      home: HomeShell(
        tenantId: _tenantId,
        onNavigate: (screen) {
          _navigatorKey.currentState?.push(
            MaterialPageRoute(builder: (_) => screen),
          );
        },
      ),
    );
  }
}

/// پوستهٔ اصلی — Bottom Navigation بین «فروشگاه» و «سبد خرید».
class HomeShell extends StatefulWidget {
  const HomeShell({super.key, this.tenantId, this.onNavigate});

  final String? tenantId;
  final void Function(Widget screen)? onNavigate;

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  int _index = 0;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: _index,
        children: [
          HomeScreen(tenantId: widget.tenantId),
          CartScreen(tenantId: widget.tenantId),
        ],
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: (i) => setState(() => _index = i),
        destinations: const [
          NavigationDestination(icon: Icon(Icons.storefront), label: 'فروشگاه'),
          NavigationDestination(icon: Icon(Icons.shopping_cart), label: 'سبد خرید'),
        ],
      ),
    );
  }
}
