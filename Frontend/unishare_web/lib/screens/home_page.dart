import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../services/api_service.dart';
import 'login_page.dart';
import 'product_page.dart';
import 'conversations_page.dart'; // Import necesar pentru mesaje

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  // Date
  List<dynamic> items = [];
  List<dynamic> filteredItems = [];
  bool isLoading = true;

  // Stare pentru Filtrare și Căutare
  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';

  // Seturi pentru selecție multiplă
  final Set<int> _selectedCategories = {};
  final Set<int> _selectedConditions = {};

  String _sortOption = 'name_asc';

  // Mapări (Category & Condition)
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

  // Mapare pentru Sortare
  static const Map<String, String> _sortMap = {
    'name_asc': 'Name (A-Z)',
    'name_desc': 'Name (Z-A)',
  };

  String _mapIntOrStringToName(dynamic value, Map<int, String> map, String fallback) {
    if (value == null) return fallback;
    if (value is int) return map[value] ?? value.toString();
    if (value is String) {
      final trimmed = value.trim();
      final parsed = int.tryParse(trimmed);
      if (parsed != null) return map[parsed] ?? trimmed;
      if (map.values.any((v) => v.toLowerCase() == trimmed.toLowerCase())) return trimmed;
      return trimmed;
    }
    return fallback;
  }

  bool _matchAnyFilter(dynamic itemValue, Set<int> selectedIds, Map<int, String> map) {
    if (selectedIds.isEmpty) return true;
    if (itemValue == null) return false;

    if (selectedIds.contains(itemValue)) return true;

    for (final id in selectedIds) {
      if (itemValue.toString() == id.toString()) return true;
    }

    final itemString = itemValue.toString().trim().toLowerCase();
    for (final id in selectedIds) {
      final targetName = map[id];
      if (targetName != null && itemString == targetName.toLowerCase()) {
        return true;
      }
    }
    return false;
  }

  @override
  void initState() {
    super.initState();
    fetchItems();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> fetchItems() async {
    final result = await ApiService.getItems();
    if (mounted) {
      setState(() {
        items = result;
        _applyFilters();
        isLoading = false;
      });
    }
  }

  void _applyFilters() {
    List<dynamic> results = List.from(items);

    if (_searchQuery.isNotEmpty) {
      results = results
          .where((item) => (item['name'] ?? '')
          .toString()
          .toLowerCase()
          .contains(_searchQuery.toLowerCase()))
          .toList();
    }

    results = results.where((item) {
      return _matchAnyFilter(item['category'], _selectedCategories, _categoryMap);
    }).toList();

    results = results.where((item) {
      return _matchAnyFilter(item['condition'], _selectedConditions, _conditionMap);
    }).toList();

    results.sort((a, b) {
      String nameA = (a['name'] ?? '').toString().toLowerCase();
      String nameB = (b['name'] ?? '').toString().toLowerCase();
      if (_sortOption == 'name_desc') {
        return nameB.compareTo(nameA);
      } else {
        return nameA.compareTo(nameB);
      }
    });

    setState(() {
      filteredItems = results;
    });
  }

  // --- Helper Widgets ---
  Widget _buildSectionHeader(String title) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 24, 16, 8),
      child: Text(
        title.toUpperCase(),
        style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold, letterSpacing: 1.0, color: Colors.grey),
      ),
    );
  }

  Widget _buildRadioOption<T>({required String label, required T value, required T? groupValue, required ValueChanged<T?> onChanged}) {
    return RadioListTile<T>(
      title: Text(label, style: const TextStyle(fontSize: 14)),
      value: value,
      groupValue: groupValue,
      onChanged: onChanged,
      dense: true,
      contentPadding: const EdgeInsets.symmetric(horizontal: 16),
      activeColor: Colors.purple,
      controlAffinity: ListTileControlAffinity.leading,
    );
  }

  Widget _buildCheckboxOption({required String label, required bool value, required ValueChanged<bool?> onChanged}) {
    return CheckboxListTile(
      title: Text(label, style: const TextStyle(fontSize: 14)),
      value: value,
      onChanged: onChanged,
      dense: true,
      contentPadding: const EdgeInsets.symmetric(horizontal: 16),
      activeColor: Colors.purple,
      controlAffinity: ListTileControlAffinity.leading,
    );
  }

  // --- Sidebar (Desktop) ---
  Widget _buildFilterSidebar() {
    return Container(
      width: 280,
      color: Colors.white,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text("Filters & Sorting", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                if (_selectedCategories.isNotEmpty || _selectedConditions.isNotEmpty || _sortOption != 'name_asc')
                  TextButton(
                    onPressed: () {
                      setState(() {
                        _selectedCategories.clear();
                        _selectedConditions.clear();
                        _sortOption = 'name_asc';
                        _applyFilters();
                      });
                    },
                    child: const Text("Reset", style: TextStyle(color: Colors.redAccent)),
                  )
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: SingleChildScrollView(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _buildSectionHeader("Sorting"),
                  ..._sortMap.entries.map((entry) {
                    return _buildRadioOption<String>(
                      label: entry.value,
                      value: entry.key,
                      groupValue: _sortOption,
                      onChanged: (val) {
                        setState(() { _sortOption = val!; _applyFilters(); });
                      },
                    );
                  }),
                  const Divider(),
                  _buildSectionHeader("Categories"),
                  _buildCheckboxOption(
                    label: "All categories",
                    value: _selectedCategories.isEmpty,
                    onChanged: (val) {
                      if (val == true) { setState(() { _selectedCategories.clear(); _applyFilters(); }); }
                    },
                  ),
                  ..._categoryMap.entries.map((entry) {
                    final isSelected = _selectedCategories.contains(entry.key);
                    return _buildCheckboxOption(
                      label: entry.value,
                      value: isSelected,
                      onChanged: (val) {
                        setState(() {
                          if (val == true) _selectedCategories.add(entry.key);
                          else _selectedCategories.remove(entry.key);
                          _applyFilters();
                        });
                      },
                    );
                  }),
                  const Divider(),
                  _buildSectionHeader("Condition"),
                  _buildCheckboxOption(
                    label: "Any condition",
                    value: _selectedConditions.isEmpty,
                    onChanged: (val) {
                      if (val == true) { setState(() { _selectedConditions.clear(); _applyFilters(); }); }
                    },
                  ),
                  ..._conditionMap.entries.map((entry) {
                    final isSelected = _selectedConditions.contains(entry.key);
                    return _buildCheckboxOption(
                      label: entry.value,
                      value: isSelected,
                      onChanged: (val) {
                        setState(() {
                          if (val == true) _selectedConditions.add(entry.key);
                          else _selectedConditions.remove(entry.key);
                          _applyFilters();
                        });
                      },
                    );
                  }),
                  const SizedBox(height: 50),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  // --- Bottom Sheet (Mobile) ---
  void _showFilterSheet() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(20))),
      builder: (ctx) {
        return StatefulBuilder(
          builder: (BuildContext context, StateSetter setSheetState) {
            return Container(
              height: MediaQuery.of(context).size.height * 0.85,
              padding: const EdgeInsets.all(20.0),
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text("Filters & Sorting", style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                      IconButton(icon: const Icon(Icons.close), onPressed: () => Navigator.pop(context))
                    ],
                  ),
                  const Divider(),
                  Expanded(
                    child: SingleChildScrollView(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          _buildSectionHeader("Sorting"),
                          Wrap(
                            spacing: 8,
                            children: _sortMap.entries.map((entry) {
                              return ChoiceChip(
                                label: Text(entry.value),
                                selected: _sortOption == entry.key,
                                onSelected: (bool selected) {
                                  if (selected) setSheetState(() => _sortOption = entry.key);
                                },
                              );
                            }).toList(),
                          ),
                          _buildSectionHeader("Categories"),
                          Wrap(
                            spacing: 8,
                            children: [
                              FilterChip(
                                label: const Text("All"),
                                selected: _selectedCategories.isEmpty,
                                onSelected: (bool selected) { if(selected) setSheetState(() => _selectedCategories.clear()); },
                              ),
                              ..._categoryMap.entries.map((entry) {
                                final isSelected = _selectedCategories.contains(entry.key);
                                return FilterChip(
                                  label: Text(entry.value),
                                  selected: isSelected,
                                  onSelected: (bool selected) {
                                    setSheetState(() {
                                      if (selected) _selectedCategories.add(entry.key);
                                      else _selectedCategories.remove(entry.key);
                                    });
                                  },
                                );
                              }),
                            ],
                          ),
                          _buildSectionHeader("Condition"),
                          Wrap(
                            spacing: 8,
                            children: [
                              FilterChip(
                                label: const Text("All"),
                                selected: _selectedConditions.isEmpty,
                                onSelected: (bool selected) { if(selected) setSheetState(() => _selectedConditions.clear()); },
                              ),
                              ..._conditionMap.entries.map((entry) {
                                final isSelected = _selectedConditions.contains(entry.key);
                                return FilterChip(
                                  label: Text(entry.value),
                                  selected: isSelected,
                                  onSelected: (bool selected) {
                                    setSheetState(() {
                                      if (selected) _selectedConditions.add(entry.key);
                                      else _selectedConditions.remove(entry.key);
                                    });
                                  },
                                );
                              }),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton(
                          onPressed: () {
                            setSheetState(() {
                              _selectedCategories.clear();
                              _selectedConditions.clear();
                              _sortOption = 'name_asc';
                            });
                          },
                          child: const Text("Reset All"),
                        ),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: ElevatedButton(
                          style: ElevatedButton.styleFrom(backgroundColor: Colors.blue, foregroundColor: Colors.white),
                          onPressed: () { _applyFilters(); Navigator.pop(context); },
                          child: const Text("Apply"),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            );
          },
        );
      },
    );
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
    final screenWidth = MediaQuery.of(context).size.width;
    final bool isWideScreen = screenWidth > 800;

    return Scaffold(
      backgroundColor: Colors.grey[100],
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 1,
        // Butonul de meniu (Sidebar global / Drawer)
        leading: IconButton(
          icon: const Icon(Icons.menu, color: Colors.black),
          // Aceasta va deschide Drawer-ul din părintele (MainPage)
          onPressed: () => Scaffold.of(context).openDrawer(),
        ),
        title: Row(
          children: [
            if (isWideScreen) ...[
              const Text(
                "UniShare",
                style: TextStyle(
                  color: Colors.deepPurple,
                  fontWeight: FontWeight.bold,
                  fontSize: 22,
                ),
              ),
              const SizedBox(width: 4),
              Text(
                "Web",
                style: TextStyle(
                  color: Colors.grey[600],
                  fontWeight: FontWeight.w400,
                  fontSize: 22,
                ),
              ),
              const SizedBox(width: 40),
            ],
            Expanded(
              child: Container(
                height: 40,
                decoration: BoxDecoration(
                  color: Colors.grey[200],
                  borderRadius: BorderRadius.circular(8),
                ),
                child: TextField(
                  controller: _searchController,
                  onChanged: (value) {
                    _searchQuery = value;
                    _applyFilters();
                  },
                  decoration: const InputDecoration(
                    hintText: 'Search items...',
                    border: InputBorder.none,
                    prefixIcon: Icon(Icons.search, color: Colors.grey),
                    contentPadding: EdgeInsets.symmetric(vertical: 8),
                  ),
                ),
              ),
            ),
          ],
        ),
        actions: [
          if (!isWideScreen)
            Stack(
              children: [
                IconButton(
                  icon: const Icon(Icons.tune, color: Colors.black),
                  tooltip: 'Filters & Sorting',
                  onPressed: _showFilterSheet,
                ),
                if (_selectedCategories.isNotEmpty || _selectedConditions.isNotEmpty || _sortOption != 'name_asc')
                  Positioned(
                    right: 8, top: 8,
                    child: Container(width: 10, height: 10, decoration: const BoxDecoration(color: Colors.blue, shape: BoxShape.circle)),
                  )
              ],
            ),
          IconButton(
            icon: const Icon(Icons.message, color: Colors.black),
            onPressed: () {
              Navigator.push(
                context,
                MaterialPageRoute(builder: (context) => const ConversationsPage()),
              );
            },
          ),
          IconButton(
            icon: const Icon(Icons.logout, color: Colors.redAccent),
            tooltip: 'Logout',
            onPressed: _logout,
          ),
        ],
      ),

      body: isLoading
          ? const Center(child: CircularProgressIndicator())
          : Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (isWideScreen) ...[
            _buildFilterSidebar(),
            Container(width: 1, color: Colors.grey[300]),
          ],
          Expanded(
            child: filteredItems.isEmpty
                ? Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.search_off, size: 64, color: Colors.grey),
                  const SizedBox(height: 16),
                  Text('No items found.', style: TextStyle(fontSize: 18, color: Colors.grey[600])),
                  TextButton(
                    onPressed: () {
                      setState(() {
                        _selectedCategories.clear();
                        _selectedConditions.clear();
                        _sortOption = 'name_asc';
                        _searchController.clear();
                        _searchQuery = '';
                        _applyFilters();
                      });
                    },
                    child: const Text("Reset filters"),
                  )
                ],
              ),
            )
                : Padding(
              padding: const EdgeInsets.all(12.0),
              child: LayoutBuilder(
                builder: (context, constraints) {
                  double availableWidth = constraints.maxWidth;
                  int crossAxisCount = 2;
                  if (availableWidth > 600) crossAxisCount = 3;
                  if (availableWidth > 900) crossAxisCount = 4;
                  if (availableWidth > 1200) crossAxisCount = 5;

                  return GridView.builder(
                    itemCount: filteredItems.length,
                    gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: crossAxisCount,
                      crossAxisSpacing: 12,
                      mainAxisSpacing: 12,
                      childAspectRatio: 0.8,
                    ),
                    itemBuilder: (context, index) {
                      final item = filteredItems[index];
                      final categoryText = _mapIntOrStringToName(item['category'], _categoryMap, 'Unknown');
                      final conditionText = _mapIntOrStringToName(item['condition'], _conditionMap, 'Unknown');

                      return Card(
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                        elevation: 2,
                        clipBehavior: Clip.antiAlias,
                        child: InkWell(
                          onTap: () {
                            Navigator.push(context, MaterialPageRoute(builder: (_) => ProductPage(itemId: item['id'].toString())));
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
                                  errorBuilder: (context, error, stackTrace) => Container(color: Colors.grey[300], child: const Icon(Icons.broken_image, size: 40, color: Colors.grey)),
                                )
                                    : Container(color: Colors.grey[300], child: const Icon(Icons.image_not_supported, size: 40, color: Colors.grey)),
                              ),
                              Padding(
                                padding: const EdgeInsets.all(8.0),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(item['name'] ?? 'Unnamed item', style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold), overflow: TextOverflow.ellipsis, maxLines: 1),
                                    const SizedBox(height: 4),
                                    Text(categoryText, style: TextStyle(color: Colors.grey[700], fontSize: 13), maxLines: 1, overflow: TextOverflow.ellipsis),
                                    const SizedBox(height: 4),
                                    Text(conditionText, style: const TextStyle(color: Colors.blueGrey, fontSize: 12)),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      );
                    },
                  );
                },
              ),
            ),
          ),
        ],
      ),
    );
  }
}