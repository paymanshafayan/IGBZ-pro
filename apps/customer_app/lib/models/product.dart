class ProductSummary {
  ProductSummary({
    required this.id,
    required this.slug,
    required this.name,
    this.imageUrl,
    required this.priceToman,
    this.oldPriceToman,
  });

  final String id;
  final String slug;
  final String name;
  final String? imageUrl;
  final double priceToman;
  final double? oldPriceToman;

  factory ProductSummary.fromJson(Map<String, dynamic> json) => ProductSummary(
        id: json['id'] as String,
        slug: json['slug'] as String,
        name: json['name'] as String,
        imageUrl: json['imageUrl'] as String?,
        priceToman: (json['priceToman'] as num).toDouble(),
        oldPriceToman: (json['oldPriceToman'] as num?)?.toDouble(),
      );
}

class ProductVariant {
  ProductVariant({
    required this.sku,
    this.attributes = const {},
    required this.priceToman,
    this.oldPriceToman,
    required this.availableQuantity,
  });

  final String sku;
  final Map<String, String> attributes;
  final double priceToman;
  final double? oldPriceToman;
  final int availableQuantity;

  factory ProductVariant.fromJson(Map<String, dynamic> json) => ProductVariant(
        sku: json['sku'] as String,
        attributes: (json['attributes'] as Map<String, dynamic>? ?? {})
            .map((k, v) => MapEntry(k, v.toString())),
        priceToman: (json['priceToman'] as num).toDouble(),
        oldPriceToman: (json['oldPriceToman'] as num?)?.toDouble(),
        availableQuantity: json['availableQuantity'] as int? ?? 0,
      );
}

class ProductDetail {
  ProductDetail({
    required this.id,
    required this.slug,
    required this.name,
    this.description,
    this.images = const [],
    this.variants = const [],
    required this.isDigital,
  });

  final String id;
  final String slug;
  final String name;
  final String? description;
  final List<String> images;
  final List<ProductVariant> variants;
  final bool isDigital;

  factory ProductDetail.fromJson(Map<String, dynamic> json) => ProductDetail(
        id: json['id'] as String,
        slug: json['slug'] as String,
        name: json['name'] as String,
        description: json['description'] as String?,
        images: (json['images'] as List<dynamic>? ?? []).cast<String>(),
        variants: (json['variants'] as List<dynamic>? ?? [])
            .map((e) => ProductVariant.fromJson(e as Map<String, dynamic>))
            .toList(),
        isDigital: json['isDigital'] as bool? ?? false,
      );
}
