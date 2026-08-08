import 'package:flutter/material.dart';

import '../api/api_client.dart';
import '../models/product.dart';
import 'product_edit_screen.dart';

/// لیست محصولات فروشگاه + افزودن/ویرایش/حذف/انتشار.
class ProductsScreen extends StatefulWidget {
  const ProductsScreen({super.key});

  @override
  State<ProductsScreen> createState() => _ProductsScreenState();
}

class _ProductsScreenState extends State<ProductsScreen> {
  final _api = ApiClient();
  List<Product> _products = [];
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
      // ادمین همهٔ محصولات (شامل منتشرنشده) را از endpoint مخصوص ادمین می‌بیند
      final data = await _api.get('/admin/products');
      setState(() {
        _products = (data['products'] as List<dynamic>? ?? [])
            .map((e) => Product.fromJson(e as Map<String, dynamic>))
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

  Future<void> _togglePublish(Product product) async {
    try {
      await _api.post('/admin/products/${product.id}/publish', {
        'published': !product.isPublished,
      });
      await _load();
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
    }
  }

  Future<void> _delete(Product product) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('حذف محصول'),
        content: Text('«${product.name}» حذف شود؟'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('انصراف')),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('حذف')),
        ],
      ),
    );

    if (confirmed != true) return;

    try {
      await _api.delete('/admin/products/${product.id}');
      await _load();
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('مدیریت محصولات')),
      floatingActionButton: FloatingActionButton(
        child: const Icon(Icons.add),
        onPressed: () async {
          await Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => const ProductEditScreen()),
          );
          await _load();
        },
      ),
      body: _busy
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
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
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: _products.isEmpty
                      ? ListView(
                          children: const [
                            Padding(
                              padding: EdgeInsets.all(32),
                              child: Text(
                                'هنوز محصولی ندارید. با دکمهٔ + شروع کنید.',
                                textAlign: TextAlign.center,
                              ),
                            ),
                          ],
                        )
                      : ListView.builder(
                          itemCount: _products.length,
                          itemBuilder: (context, index) {
                            final p = _products[index];
                            return Card(
                              child: ListTile(
                                leading: Icon(
                                  p.isPublished ? Icons.visibility : Icons.visibility_off,
                                  color: p.isPublished ? Colors.green : Colors.grey,
                                ),
                                title: Text(p.name),
                                subtitle: Text('اسلاگ: ${p.slug} — ${p.variants.length} واریانت'),
                                trailing: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    IconButton(
                                      icon: Icon(
                                        p.isPublished ? Icons.unpublished : Icons.publish,
                                      ),
                                      onPressed: () => _togglePublish(p),
                                    ),
                                    IconButton(
                                      icon: const Icon(Icons.edit),
                                      onPressed: () async {
                                        await Navigator.of(context).push(
                                          MaterialPageRoute(
                                            builder: (_) => ProductEditScreen(product: p),
                                          ),
                                        );
                                        await _load();
                                      },
                                    ),
                                    IconButton(
                                      icon: const Icon(Icons.delete_outline),
                                      onPressed: () => _delete(p),
                                    ),
                                  ],
                                ),
                              ),
                            );
                          },
                        ),
                ),
    );
  }
}
