import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../models/cart.dart';

/// تسویه — ثبت سفارش واقعی با رزرو اتمیک موجودی (با customerId واقعی از session).
class CheckoutScreen extends StatefulWidget {
  const CheckoutScreen({super.key, this.tenantId});

  final String? tenantId;

  @override
  State<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends State<CheckoutScreen> {
  final _api = ApiClient();
  bool _busy = false;
  String? _error;
  String? _successOrderId;
  double? _successTotal;

  Future<void> _placeOrder() async {
    final customerId = await _api.getCustomerId();
    if (customerId == null) {
      setState(() => _error = 'ابتدا وارد شوید.');
      return;
    }

    final lines = Cart.instance.lines;
    if (lines.isEmpty) {
      setState(() => _error = 'سبد خرید خالی است.');
      return;
    }

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      final result = await _api.post(
        '/orders',
        {
          'customerId': customerId,
          'taxRatePercent': 9,
          'lines': lines
              .map((l) => {
                    'productId': l.productId,
                    'sku': l.sku,
                    'quantity': l.quantity,
                  })
              .toList(),
        },
        tenantId: widget.tenantId,
      );

      await Cart.instance.clear();

      setState(() {
        _successOrderId = result['orderId'] as String;
        _successTotal = (result['grandTotalToman'] as num).toDouble();
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
    if (_successOrderId != null) {
      return Scaffold(
        appBar: AppBar(title: const Text('تسویه')),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.check_circle, color: Colors.green, size: 64),
                const SizedBox(height: 16),
                const Text('✅ سفارش ثبت شد', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                const SizedBox(height: 8),
                Text('شناسهٔ سفارش: $_successOrderId'),
                Text('مبلغ: ${_successTotal!.toStringAsFixed(0)} تومان'),
                const SizedBox(height: 8),
                const Text('موجودی رزرو شد. پرداخت آنلاین به‌زودی فعال می‌شود.', textAlign: TextAlign.center),
                const SizedBox(height: 20),
                ElevatedButton(
                  onPressed: () => Navigator.of(context).popUntil((r) => r.isFirst),
                  child: const Text('بازگشت به فروشگاه'),
                ),
              ],
            ),
          ),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(title: const Text('تسویه')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const Text('سفارش شما', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          ...Cart.instance.lines.map((l) => ListTile(
                contentPadding: EdgeInsets.zero,
                title: Text(l.name),
                trailing: Text('${l.total.toStringAsFixed(0)} تومان'),
              )),
          const Divider(),
          ListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('جمع کل', style: TextStyle(fontWeight: FontWeight.bold)),
            trailing: Text(
              '${Cart.instance.total.toStringAsFixed(0)} تومان',
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
          ),
          if (_error != null) ...[
            const SizedBox(height: 12),
            Text(_error!, style: const TextStyle(color: Colors.red)),
          ],
          const SizedBox(height: 24),
          ElevatedButton(
            onPressed: _busy ? null : _placeOrder,
            style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 14)),
            child: Text(_busy ? 'در حال ثبت…' : 'ثبت سفارش'),
          ),
        ],
      ),
    );
  }
}
