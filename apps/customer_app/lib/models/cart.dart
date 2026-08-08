import 'package:shared_preferences/shared_preferences.dart';

/// خط سبد خرید در-حافظهٔ اپ.
class CartLine {
  CartLine({
    required this.productId,
    required this.sku,
    required this.name,
    required this.priceToman,
    this.quantity = 1,
  });

  final String productId;
  final String sku;
  final String name;
  final double priceToman;
  int quantity;

  double get total => priceToman * quantity;
}

/// سبد خرید — ذخیره در SharedPreferences.
class Cart {
  Cart._();

  static final Cart instance = Cart._();

  final List<CartLine> _lines = [];
  static const _key = 'igbz_cart_lines';

  List<CartLine> get lines => List.unmodifiable(_lines);

  double get total => _lines.fold(0, (sum, l) => sum + l.total);

  Future<void> load() async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString(_key);
    if (raw == null) return;

    try {
      final list = (raw as List).cast<Map<String, dynamic>>();
      _lines
        ..clear()
        ..addAll(list.map((e) => CartLine(
              productId: e['productId'] as String,
              sku: e['sku'] as String,
              name: e['name'] as String,
              priceToman: (e['priceToman'] as num).toDouble(),
              quantity: e['quantity'] as int? ?? 1,
            )));
    } catch (_) {
      _lines.clear();
    }
  }

  Future<void> _save() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(
      _key,
      _lines
          .map((l) => {
                'productId': l.productId,
                'sku': l.sku,
                'name': l.name,
                'priceToman': l.priceToman,
                'quantity': l.quantity,
              })
          .toList()
          .toString(),
    );
  }

  Future<void> add(CartLine line) async {
    final existing = _lines.where((l) => l.sku == line.sku).firstOrNull;
    if (existing != null) {
      existing.quantity += line.quantity;
    } else {
      _lines.add(line);
    }
    await _save();
  }

  Future<void> updateQuantity(String sku, int quantity) async {
    final line = _lines.where((l) => l.sku == sku).firstOrNull;
    if (line != null) {
      if (quantity <= 0) {
        _lines.remove(line);
      } else {
        line.quantity = quantity;
      }
      await _save();
    }
  }

  Future<void> clear() async {
    _lines.clear();
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_key);
  }
}

extension FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}
