import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../models/cart.dart';
import '../models/product.dart';

/// صفحهٔ جزئیات محصول + انتخاب واریانت + افزودن به سبد.
class ProductScreen extends StatefulWidget {
  const ProductScreen({super.key, required this.slug, this.tenantId});

  final String slug;
  final String? tenantId;

  @override
  State<ProductScreen> createState() => _ProductScreenState();
}

class _ProductScreenState extends State<ProductScreen> {
  final _api = ApiClient();
  ProductDetail? _product;
  String? _selectedSku;
  int _quantity = 1;
  bool _busy = true;
  String? _error;
  bool _added = false;

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
      final data = await _api.get('/catalog/products/${widget.slug}', tenantId: widget.tenantId);
      final product = ProductDetail.fromJson(data['product'] as Map<String, dynamic>);
      setState(() {
        _product = product;
        _selectedSku = product.variants.isNotEmpty ? product.variants.first.sku : null;
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

  ProductVariant? get _selectedVariant {
    final p = _product;
    if (p == null) return null;
    return p.variants.where((v) => v.sku == _selectedSku).firstOrNull ?? p.variants.firstOrNull;
  }

  Future<void> _addToCart() async {
    final p = _product;
    final v = _selectedVariant;
    if (p == null || v == null) return;

    await Cart.instance.add(CartLine(
      productId: p.id,
      sku: v.sku,
      name: p.name,
      priceToman: v.priceToman,
      quantity: _quantity,
    ));

    setState(() => _added = true);
    Future.delayed(const Duration(milliseconds: 1500), () {
      if (mounted) setState(() => _added = false);
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('محصول')),
      body: _busy
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(child: Text(_error!, style: const TextStyle(color: Colors.red)))
              : _buildDetail(),
    );
  }

  Widget _buildDetail() {
    final p = _product!;
    final v = _selectedVariant;

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (p.images.isNotEmpty)
          ClipRRect(
            borderRadius: BorderRadius.circular(12),
            child: Image.network(p.images.first, height: 280, fit: BoxFit.cover),
          )
        else
          Container(height: 280, color: Colors.grey.shade200),
        const SizedBox(height: 16),
        Text(p.name, style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold)),
        if (p.description != null) ...[
          const SizedBox(height: 8),
          Text(p.description!),
        ],
        const SizedBox(height: 16),
        if (v != null)
          Row(
            children: [
              Text(
                '${v.priceToman.toStringAsFixed(0)} تومان',
                style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Colors.deepPurple),
              ),
              if (v.oldPriceToman != null && v.oldPriceToman! > v.priceToman) ...[
                const SizedBox(width: 8),
                Text(
                  '${v.oldPriceToman!.toStringAsFixed(0)}',
                  style: TextStyle(
                    color: Colors.grey,
                    decoration: TextDecoration.lineThrough,
                  ),
                ),
              ],
            ],
          ),
        if (p.variants.length > 1) ...[
          const SizedBox(height: 16),
          DropdownButtonFormField<String>(
            initialValue: _selectedSku,
            decoration: const InputDecoration(labelText: 'انتخاب مدل', border: OutlineInputBorder()),
            items: p.variants
                .map((x) => DropdownMenuItem(
                      value: x.sku,
                      child: Text(x.attributes['color'] ?? x.attributes['size'] ?? x.sku),
                    ))
                .toList(),
            onChanged: (sku) => setState(() => _selectedSku = sku),
          ),
        ],
        const SizedBox(height: 16),
        Row(
          children: [
            const Text('تعداد: '),
            IconButton(
              icon: const Icon(Icons.remove_circle_outline),
              onPressed: () => setState(() => _quantity = _quantity > 1 ? _quantity - 1 : 1),
            ),
            Text('$_quantity'),
            IconButton(
              icon: const Icon(Icons.add_circle_outline),
              onPressed: () => setState(() => _quantity = _quantity + 1),
            ),
          ],
        ),
        const SizedBox(height: 16),
        ElevatedButton(
          onPressed: _addToCart,
          style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 14)),
          child: const Text('افزودن به سبد خرید'),
        ),
        if (_added)
          const Padding(
            padding: EdgeInsets.only(top: 8),
            child: Text('✓ به سبد خرید اضافه شد', style: TextStyle(color: Colors.green)),
          ),
      ],
    );
  }
}
