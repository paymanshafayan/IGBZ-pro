import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../models/product.dart';

/// فرم افزودن/ویرایش محصول — نام، اسلاگ، توضیح، تصویر و واریانت‌ها.
class ProductEditScreen extends StatefulWidget {
  const ProductEditScreen({super.key, this.product});

  final Product? product;

  @override
  State<ProductEditScreen> createState() => _ProductEditScreenState();
}

class _ProductEditScreenState extends State<ProductEditScreen> {
  final _api = ApiClient();
  final _nameCtrl = TextEditingController();
  final _slugCtrl = TextEditingController();
  final _descCtrl = TextEditingController();
  final _imageCtrl = TextEditingController();

  bool _isPublished = false;
  bool _busy = false;
  String? _error;

  // واریانت‌ها (ساده: SKU + قیمت + موجودی)
  final List<Map<String, String>> _variants = [];

  bool get _isEdit => widget.product != null;

  @override
  void initState() {
    super.initState();
    final p = widget.product;
    if (p != null) {
      _nameCtrl.text = p.name;
      _slugCtrl.text = p.slug;
      _descCtrl.text = p.description ?? '';
      _imageCtrl.text = p.images.isNotEmpty ? p.images.first : '';
      _isPublished = p.isPublished;
      for (final v in p.variants) {
        _variants.add({
          'sku': v.sku,
          'price': v.priceToman.toString(),
          'stock': v.stockQuantity.toString(),
        });
      }
    } else {
      _variants.add({'sku': '', 'price': '', 'stock': ''});
    }
  }

  Future<void> _save() async {
    final name = _nameCtrl.text.trim();
    final slug = _slugCtrl.text.trim();

    if (name.isEmpty || slug.isEmpty) {
      setState(() => _error = 'نام و اسلاگ الزامی است.');
      return;
    }

    final variants = _variants
        .where((v) => (v['sku'] ?? '').trim().isNotEmpty)
        .map((v) => {
              'sku': (v['sku'] ?? '').trim(),
              'attributes': <String, String>{},
              'priceToman': double.tryParse(v['price'] ?? '0') ?? 0,
              'oldPriceToman': null,
              'stockQuantity': int.tryParse(v['stock'] ?? '0') ?? 0,
            })
        .toList();

    if (variants.isEmpty) {
      setState(() => _error = 'حداقل یک واریانت با SKU لازم است.');
      return;
    }

    final input = ProductInput(
      name: name,
      slug: slug,
      description: _descCtrl.text.trim().isEmpty ? null : _descCtrl.text.trim(),
      images: _imageCtrl.text.trim().isEmpty ? const [] : [_imageCtrl.text.trim()],
      isPublished: _isPublished,
      variants: variants
          .map((v) => ProductVariant(
                sku: v['sku'] as String,
                priceToman: v['priceToman'] as double,
                stockQuantity: v['stockQuantity'] as int,
              ))
          .toList(),
    );

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      if (_isEdit) {
        await _api.put('/admin/products/${widget.product!.id}', input.toJson());
      } else {
        await _api.post('/admin/products', input.toJson());
      }
      if (mounted) {
        Navigator.of(context).pop(true);
      }
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } catch (e) {
      setState(() => _error = 'خطا: $e');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _addVariantRow() {
    setState(() => _variants.add({'sku': '', 'price': '', 'stock': ''}));
  }

  void _removeVariantRow(int index) {
    setState(() => _variants.removeAt(index));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_isEdit ? 'ویرایش محصول' : 'محصول جدید')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          TextField(
            controller: _nameCtrl,
            decoration: const InputDecoration(labelText: 'نام محصول', border: OutlineInputBorder()),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: _slugCtrl,
            decoration: const InputDecoration(
              labelText: 'اسلاگ (برای URL)',
              border: OutlineInputBorder(),
              hintText: 'مثل t-shirt-red',
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: _descCtrl,
            maxLines: 3,
            decoration: const InputDecoration(labelText: 'توضیحات', border: OutlineInputBorder()),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: _imageCtrl,
            decoration: const InputDecoration(
              labelText: 'آدرس تصویر (URL)',
              border: OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          SwitchListTile(
            title: const Text('منتشرشده (نمایش در فروشگاه)'),
            value: _isPublished,
            onChanged: (v) => setState(() => _isPublished = v),
            contentPadding: EdgeInsets.zero,
          ),
          const SizedBox(height: 16),
          const Text('واریانت‌ها', style: TextStyle(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          ..._variants.asMap().entries.map((entry) {
            final index = entry.key;
            final v = entry.value;
            return Card(
              child: Padding(
                padding: const EdgeInsets.all(8),
                child: Row(
                  children: [
                    Expanded(
                      child: TextField(
                        decoration: const InputDecoration(labelText: 'SKU'),
                        onChanged: (s) => v['sku'] = s,
                        initialValue: v['sku'],
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: TextField(
                        decoration: const InputDecoration(labelText: 'قیمت'),
                        keyboardType: TextInputType.number,
                        onChanged: (s) => v['price'] = s,
                        initialValue: v['price'],
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: TextField(
                        decoration: const InputDecoration(labelText: 'موجودی'),
                        keyboardType: TextInputType.number,
                        onChanged: (s) => v['stock'] = s,
                        initialValue: v['stock'],
                      ),
                    ),
                    IconButton(
                      icon: const Icon(Icons.remove_circle_outline),
                      onPressed: _variants.length > 1 ? () => _removeVariantRow(index) : null,
                    ),
                  ],
                ),
              ),
            );
          }),
          TextButton.icon(
            onPressed: _addVariantRow,
            icon: const Icon(Icons.add),
            label: const Text('افزودن واریانت'),
          ),
          if (_error != null) ...[
            const SizedBox(height: 12),
            Text(_error!, style: const TextStyle(color: Colors.red)),
          ],
          const SizedBox(height: 24),
          ElevatedButton(
            onPressed: _busy ? null : _save,
            style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 14)),
            child: Text(_busy ? 'در حال ذخیره…' : 'ذخیره'),
          ),
        ],
      ),
    );
  }
}
