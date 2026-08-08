class OrderItem {
  OrderItem({
    required this.id,
    required this.customerId,
    required this.status,
    required this.grandTotalToman,
    required this.createdOnUtc,
  });

  final String id;
  final String customerId;
  final String status;
  final double grandTotalToman;
  final String createdOnUtc;

  factory OrderItem.fromJson(Map<String, dynamic> json) => OrderItem(
        id: json['id'] as String,
        customerId: json['customerId'] as String,
        status: json['status'] as String,
        grandTotalToman: (json['grandTotalToman'] as num).toDouble(),
        createdOnUtc: json['createdOnUtc'] as String? ?? '',
      );
}

class DashboardSummary {
  DashboardSummary({
    required this.productCount,
    required this.orderCount,
    required this.paidOrderCount,
    required this.revenueToman,
    required this.customerCount,
  });

  final int productCount;
  final int orderCount;
  final int paidOrderCount;
  final double revenueToman;
  final int customerCount;

  factory DashboardSummary.fromJson(Map<String, dynamic> json) => DashboardSummary(
        productCount: json['productCount'] as int? ?? 0,
        orderCount: json['orderCount'] as int? ?? 0,
        paidOrderCount: json['paidOrderCount'] as int? ?? 0,
        revenueToman: (json['revenueToman'] as num?)?.toDouble() ?? 0,
        customerCount: json['customerCount'] as int? ?? 0,
      );
}
