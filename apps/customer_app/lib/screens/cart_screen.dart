import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../models/cart.dart';
import 'checkout_screen.dart';
import 'login_screen.dart';

/// صفحهٔ سبد خرید — تغییر تعداد، حذف، تسویه.
class CartScreen extends StatefulWidget {
  const CartScreen({super.key, this.tenantId});

  final String? tenantId;

  @override
  State<CartScreen> createState() => _CartScreenState();
}

class _CartScreenState extends State<CartScreen> {
  final _api = ApiClient();
  int _revision = 0;

  void _bump() => setState(() => _revision++);

  Future<void> _checkout() async {
    final customerId = await _api.getCustomerId();

    if (customerId == null) {
      if (!mounted) return;
      // ورود الزامی برای تسویه
      final loggedIn = await Navigator.of(context).push<bool>(
        MaterialPageRoute(builder: (_) => LoginScreen(tenantId: widget.tenantId)),
      );
      if (loggedIn != true) return;
    }

    if (!mounted) return;
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => CheckoutScreen(tenantId: widget.tenantId),
      ),
    );
    _bump();
  }

  @override
  Widget build(BuildContext context) {
    final lines = Cart.instance.lines;
    final total = Cart.instance.total;

    return Scaffold(
      appBar: AppBar(title: const Text('سبد خرید')),
      body: lines.isEmpty
          ? const Center(child: Text('سبد خرید شما خالی است.'))
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                ...lines.map((line) => Card(
                      child: ListTile(
                        title: Text(line.name),
                        subtitle: Text('${line.priceToman.toStringAsFixed(0)} تومان'),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            IconButton(
                              icon: const Icon(Icons.remove_circle_outline),
                              onPressed: () async {
                                await Cart.instance.updateQuantity(line.sku, line.quantity - 1);
                                _bump();
                              },
                            ),
                            Text('${line.quantity}'),
                            IconButton(
                              icon: const Icon(Icons.add_circle_outline),
                              onPressed: () async {
                                await Cart.instance.updateQuantity(line.sku, line.quantity + 1);
                                _bump();
                              },
                            ),
                            IconButton(
                              icon: const Icon(Icons.delete_outline),
                              onPressed: () async {
                                await Cart.instance.updateQuantity(line.sku, 0);
                                _bump();
                              },
                            ),
                          ],
                        ),
                      ),
                    )),
                const SizedBox(height: 16),
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('جمع کل', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                        Text(
                          '${total.toStringAsFixed(0)} تومان',
                          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                ElevatedButton(
                  onPressed: _checkout,
                  style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 14)),
                  child: const Text('ادامهٔ خرید (تسویه)'),
                ),
              ],
            ),
    );
  }
}
