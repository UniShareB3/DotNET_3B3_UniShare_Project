import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../services/api_service.dart';
import 'login_page.dart';
import 'product_page.dart';

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  List<dynamic> items = [];
  bool isLoading = true;

  // Use numeric-keyed maps aligned with backend enums and a helper mapper
  static const Map<int, String> _categoryMap = {
    0: 'Others',
    1: 'Books',
    2: 'Electronics',
    3: 'Kitchen',
    4: 'Clothing',
    5: 'Accessories',
  };

  static const Map<int, String> _conditionMap = {
    0: 'New',
    1: 'Excellent',
    2: 'Good',
    3: 'Fair',
    4: 'Poor',
  };

  String _mapIntOrStringToName(dynamic value, Map<int, String> map, String fallback) {
    if (value == null) return fallback;
    if (value is int) return map[value] ?? value.toString();
    if (value is String) {
      final trimmed = value.trim();
      // numeric-as-string
      final parsed = int.tryParse(trimmed);
      if (parsed != null) return map[parsed] ?? trimmed;
      // if backend already returned the display name (case-insensitive match), return it
      if (map.values.any((v) => v.toLowerCase() == trimmed.toLowerCase())) return trimmed;
      return trimmed; // fallback to the raw string
    }
    return fallback;
  }


  @override
  void initState() {
    super.initState();
    fetchItems();
  }

  Future<void> fetchItems() async {
    final result = await ApiService.getItems();
    setState(() {
      items = result;
      isLoading = false;
    });
  }

  void _logout() {
    final auth = context.read<AuthProvider>();
    auth.logout();
    Navigator.pushAndRemoveUntil(
      context,
      MaterialPageRoute(builder: (_) => const LoginPage()),
          (route) => false,
    );
  }

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.of(context).size.width;

    int crossAxisCount = 2;
    if (width > 600) crossAxisCount = 3;
    if (width > 900) crossAxisCount = 4;

    return isLoading
        ? const Center(child: CircularProgressIndicator())
        : Padding(
      padding: const EdgeInsets.all(12.0),
      child: GridView.builder(
        itemCount: items.length,
        gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
          crossAxisCount: crossAxisCount,
          crossAxisSpacing: 12,
          mainAxisSpacing: 12,
          childAspectRatio: 0.8,
        ),
        itemBuilder: (context, index) {
          final item = items[index];
          final categoryText = _mapIntOrStringToName(item['category'], _categoryMap, 'Unknown');
          final conditionText = _mapIntOrStringToName(item['condition'], _conditionMap, 'Unknown');

          return Card(
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(16),
            ),
            elevation: 2,
            clipBehavior: Clip.antiAlias,
            child: InkWell(
              onTap: () {
                // Open the product page directly and pass the item id as a constructor parameter.
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => ProductPage(itemId: item['id'].toString()),
                  ),
                );
              },
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  AspectRatio(
                    aspectRatio: 1.3,
                    child: (item['imageUrl'] != null && (item['imageUrl'] as String).trim().isNotEmpty)
                        ? Image.network(
                      item['imageUrl'],
                      fit: BoxFit.cover,
                      errorBuilder: (context, error, stackTrace) => Container(
                        color: Colors.grey[300],
                        child: const Icon(
                          Icons.broken_image,
                          size: 40,
                          color: Colors.grey,
                        ),
                      ),
                    )
                        : Container(
                      color: Colors.grey[300],
                      child: const Icon(
                        Icons.image_not_supported,
                        size: 40,
                        color: Colors.grey,
                      ),
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.all(8.0),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          item['name'] ?? 'Unnamed item',
                          style: const TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.bold,
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 4),
                        Text(
                          categoryText,
                          style: TextStyle(
                            color: Colors.grey[700],
                            fontSize: 13,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          conditionText,
                          style: const TextStyle(
                            color: Colors.blueGrey,
                            fontSize: 12,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}
