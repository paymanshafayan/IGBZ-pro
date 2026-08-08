import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../models/order.dart';
import 'login_screen.dart';

/// داشبورد ادمین — آمار + سفارش‌ها + دکمهٔ مدیریت محصولات.
class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  final _api = ApiClient();
  DashboardSummary? _summary;
  List<OrderItem> _orders = [];
  bool _busy = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      final summaryData = await _api.get('/admin/dashboard/summary');
      final ordersData = await _api.get('/admin/dashboard/orders');

      setState(() {
        _summary = DashboardSummary.fromJson(
          (summaryData['summary'] as Map<String, dynamic>?) ?? {},
        );
        _orders = (ordersData['orders'] as List<dynamic>? ?? [])
            .map((e) => OrderItem.fromJson(e as Map<String, dynamic>))
            .toList();
        _busy = false;
      });
    } on ApiException catch (e) {
      setState(() {
        _error = e.message;
        _busy = false;
      });
    } catch (e) {
      setState(() {
        _error = 'خطا: $e';
        _busy = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('داشبورد فروشگاه'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _load,
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () async {
              await _api.clearSession();
              if (!mounted) return;
              Navigator.of(context).pushAndRemoveUntil(
                MaterialPageRoute(
                  builder: (_) => LoginScreen(
                    onLoggedIn: () {},
                  ),
                ),
                (_) => false,
              );
            },
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _load,
        child: _buildBody(),
      ),
    );
  }

  Widget _buildBody() {
    if (_busy) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(_error!, style: const TextStyle(color: Colors.red)),
              const SizedBox(height: 12),
              ElevatedButton(onPressed: _load, child: const Text('تلاش مجدد')),
            ],
          ),
        ),
      );
    }

    final s = _summary;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (s != null)
          GridView.count(
            crossAxisCount: 2,
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            childAspectRatio: 1.6,
            children: [
              _StatCard(label: 'محصولات', value: '${s.productCount}', icon: Icons.inventory_2),
              _StatCard(label: 'سفارش‌ها', value: '${s.orderCount}', icon: Icons.receipt_long),
              _StatCard(label: 'پرداخت‌شده', value: '${s.paidOrderCount}', icon: Icons.check_circle),
              _StatCard(
                label: 'درآمد',
                value: '${s.revenueToman.toStringAsFixed(0)}',
                icon: Icons.payments,
              ),
            ],
          ),
        const SizedBox(height: 16),
        ElevatedButton.icon(
          onPressed: () {
            Navigator.of(context).push(
              MaterialPageRoute(builder: (_) => const ProductsScreen()),
            );
          },
          icon: const Icon(Icons.add_business),
          label: const Text('مدیریت محصولات'),
          style: ElevatedButton.styleFrom(
            padding: const EdgeInsets.symmetric(vertical: 14),
          ),
        ),
        const SizedBox(height: 24),
        const Text(
          'سفارش‌های اخیر',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 8),
        if (_orders.isEmpty)
          const Padding(
            padding: EdgeInsets.all(16),
            child: Text('هنوز سفارشی ثبت نشده است.'),
          )
        else
          ..._orders.map(
            (o) => Card(
              child: ListTile(
                leading: const Icon(Icons.receipt),
                title: Text('سفارش #${o.id.substring(0, 8)}'),
                subtitle: Text('مشتری: ${o.customerId}'),
                trailing: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      '${o.grandTotalToman.toStringAsFixed(0)} تومان',
                      style: const TextStyle(fontWeight: FontWeight.bold),
                    ),
                    Text(o.status, style: const TextStyle(fontSize: 12)),
                  ],
                ),
              ),
            ),
          ),
      ],
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({
    required this.label,
    required this.value,
    required this.icon,
  });

  final String label;
  final String value;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, color: Colors.deepPurple),
            const SizedBox(height: 8),
            Text(value, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
            Text(label, style: const TextStyle(color: Colors.grey)),
          ],
        ),
      ),
    );
  }
}
