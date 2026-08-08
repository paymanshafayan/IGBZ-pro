class ProductVariant {
  ProductVariant({
    required this.sku,
    this.attributes = const {},
    required this.priceToman,
    this.oldPriceToman,
    this.stockQuantity = 0,
  });

  final String sku;
  final Map<String, String> attributes;
  final double priceToman;
  final double? oldPriceToman;
  final int stockQuantity;

  factory ProductVariant.fromJson(Map<String, dynamic> json) => ProductVariant(
        sku: json['sku'] as String,
        attributes: (json['attributes'] as Map<String, dynamic>? ?? {})
            .map((k, v) => MapEntry(k, v.toString())),
        priceToman: (json['priceToman'] as num).toDouble(),
        oldPriceToman: (json['oldPriceToman'] as num?)?.toDouble(),
        stockQuantity: json['stockQuantity'] as int? ?? 0,
      );

  Map<String, dynamic> toJson() => {
        'sku': sku,
        'attributes': attributes,
        'priceToman': priceToman,
        'oldPriceToman': oldPriceToman,
        'stockQuantity': stockQuantity,
      };
}

class Product {
  Product({
    required this.id,
    required this.name,
    required this.slug,
    this.description,
    this.images = const [],
    this.isPublished = false,
    this.variants = const [],
  });

  final String id;
  final String name;
  final String slug;
  final String? description;
  final List<String> images;
  final bool isPublished;
  final List<ProductVariant> variants;

  factory Product.fromJson(Map<String, dynamic> json) => Product(
        id: json['id'] as String,
        name: json['name'] as String,
        slug: json['slug'] as String,
        description: json['description'] as String?,
        images: (json['images'] as List<dynamic>? ?? []).cast<String>(),
        isPublished: json['isPublished'] as bool? ?? false,
        variants: (json['variants'] as List<dynamic>? ?? [])
            .map((e) => ProductVariant.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class ProductInput {
  ProductInput({
    required this.name,
    required this.slug,
    this.description,
    this.images = const [],
    this.isPublished = false,
    this.isDigital = false,
    this.variants = const [],
  });

  final String name;
  final String slug;
  final String? description;
  final List<String> images;
  final bool isPublished;
  final bool isDigital;
  final List<ProductVariant> variants;

  Map<String, dynamic> toJson() => {
        'name': name,
        'slug': slug,
        'description': description,
        'images': images,
        'isPublished': isPublished,
        'isDigital': isDigital,
        'variants': variants.map((v) => v.toJson()).toList(),
      };
}
