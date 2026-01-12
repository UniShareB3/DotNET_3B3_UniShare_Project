import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart'; // Necesită pachetul image_picker în pubspec.yaml
import '../services/api_service.dart';

class EditItemPage extends StatefulWidget {
  final Map<String, dynamic> item; // Primim item-ul pe care vrem să îl edităm

  const EditItemPage({super.key, required this.item});

  @override
  State<EditItemPage> createState() => _EditItemPageState();
}

class _EditItemPageState extends State<EditItemPage> {
  final _formKey = GlobalKey<FormState>();

  // Listele de valori
  final List<String> _categories = ['Others', 'Books', 'Electronics', 'Kitchen', 'Clothing', 'Accessories'];
  final List<String> _conditions = ['New', 'Excellent', 'Good', 'Fair', 'Poor'];

  static const Map<int, String> _categoryMap = {
    0: 'Others', 1: 'Books', 2: 'Electronics', 3: 'Kitchen',
    4: 'Clothing', 5: 'Accessories',
  };

  static const Map<int, String> _conditionMap = {
    0: 'New', 1: 'Excellent', 2: 'Good', 3: 'Fair', 4: 'Poor',
  };

  late String _name;
  late String _description;
  String? _selectedCategory;
  String? _selectedCondition;
  String? _imageUrl;

  // Stare pentru imaginea selectată local
  XFile? _pickedImage;
  Uint8List? _webImageBytes; // Pentru afișare pe Web

  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _initializeFields();
  }

  void _initializeFields() {
    final item = widget.item;
    _name = item['name'] ?? '';
    _description = item['description'] ?? '';
    _imageUrl = item['imageUrl'];

    if (item['category'] is int) {
      _selectedCategory = _categoryMap[item['category']];
    } else if (item['category'] is String) {
      String cat = item['category'];
      if (_categories.contains(cat)) {
        _selectedCategory = cat;
      } else {
        int? val = int.tryParse(cat);
        if (val != null) _selectedCategory = _categoryMap[val];
      }
    }

    if (item['condition'] is int) {
      _selectedCondition = _conditionMap[item['condition']];
    } else if (item['condition'] is String) {
      String cond = item['condition'];
      if (_conditions.contains(cond)) {
        _selectedCondition = cond;
      } else {
        int? val = int.tryParse(cond);
        if (val != null) _selectedCondition = _conditionMap[val];
      }
    }

    if (!_categories.contains(_selectedCategory)) _selectedCategory = null;
    if (!_conditions.contains(_selectedCondition)) _selectedCondition = null;
  }

  // Funcție pentru a selecta imaginea din galerie
  Future<void> _pickImage() async {
    final ImagePicker picker = ImagePicker();
    try {
      final XFile? image = await picker.pickImage(source: ImageSource.gallery);
      if (image != null) {
        if (kIsWeb) {
          // Pe web citim bytes pentru a afișa
          final bytes = await image.readAsBytes();
          setState(() {
            _pickedImage = image;
            _webImageBytes = bytes;
          });
        } else {
          // Pe mobile folosim path
          setState(() {
            _pickedImage = image;
            _webImageBytes = null;
          });
        }
      }
    } catch (e) {
      print('Error picking image: $e');
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Failed to pick image')),
      );
    }
  }

  InputDecoration _getInputDecoration(String labelText, IconData icon) {
    return InputDecoration(
      labelText: labelText,
      prefixIcon: Icon(icon, color: Colors.deepPurple.shade400),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(10),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(10),
        borderSide: const BorderSide(color: Colors.deepPurple, width: 2),
      ),
    );
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    _formKey.currentState!.save();

    if (_selectedCategory == null || _selectedCondition == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text("Please select a Category and Condition")),
      );
      return;
    }

    setState(() => _isLoading = true);

    try {
      // 1. Upload Image logic (Simulat)
      if (_pickedImage != null) {
        // Aici ar trebui apelat serviciul de upload (ex: ChatService.uploadDocument sau ApiService.uploadImage)
        // await ChatService.uploadDocument(await _pickedImage!.readAsBytes(), _pickedImage!.name, 'admin_id');

        // Simulăm un upload
        await Future.delayed(const Duration(milliseconds: 800));
        // După upload, am primi un URL nou:
        // _imageUrl = "https://example.com/new_uploaded_image.jpg";
      }

      // 2. Update Item Logic (Simulat)
      // await ApiService.updateItem(...)

      await Future.delayed(const Duration(milliseconds: 500));
      const result = true;

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text("Update Mocked (Imaginea a fost selectată dar nu s-a trimis la server)")),
        );
      }

      setState(() => _isLoading = false);

      if (result == true) {
        if (mounted) {
          Navigator.pop(context, true);
        }
      }
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text("Error: $e")),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final double maxFormWidth = 600.0;

    return Scaffold(
      appBar: AppBar(
        title: const Text("Edit Item"),
        backgroundColor: Colors.deepPurple,
        foregroundColor: Colors.white,
        elevation: 0,
      ),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: BoxConstraints(maxWidth: maxFormWidth),
            child: Form(
              key: _formKey,
              autovalidateMode: AutovalidateMode.onUserInteraction,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Text(
                    'Update Details',
                    style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Colors.deepPurple),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 30),

                  // --- Image Picker Section ---
                  Center(
                    child: Column(
                      children: [
                        Container(
                          height: 200,
                          width: double.infinity,
                          decoration: BoxDecoration(
                            color: Colors.grey.shade200,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(color: Colors.grey.shade300),
                          ),
                          clipBehavior: Clip.antiAlias,
                          child: _buildImagePreview(),
                        ),
                        const SizedBox(height: 10),
                        ElevatedButton.icon(
                          onPressed: _pickImage,
                          icon: const Icon(Icons.image),
                          label: const Text('Change Image'),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: Colors.deepPurple.shade50,
                            foregroundColor: Colors.deepPurple,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Name
                  TextFormField(
                    initialValue: _name,
                    decoration: _getInputDecoration("Item Name", Icons.label_outline),
                    validator: (v) => v!.isEmpty ? "Item name is required" : null,
                    onSaved: (v) => _name = v!,
                  ),
                  const SizedBox(height: 15),

                  // Description
                  TextFormField(
                    initialValue: _description,
                    decoration: _getInputDecoration("Description", Icons.description_outlined).copyWith(
                      alignLabelWithHint: true,
                    ),
                    maxLines: 4,
                    validator: (v) => v!.isEmpty ? "Description is required" : null,
                    onSaved: (v) => _description = v!,
                  ),
                  const SizedBox(height: 15),

                  // Category
                  DropdownButtonFormField<String>(
                    value: _selectedCategory,
                    decoration: _getInputDecoration("Category", Icons.category_outlined),
                    hint: const Text("Select Item Category"),
                    items: _categories.map((String value) {
                      return DropdownMenuItem<String>(
                        value: value,
                        child: Text(value),
                      );
                    }).toList(),
                    onChanged: (String? newValue) {
                      setState(() {
                        _selectedCategory = newValue;
                      });
                    },
                    validator: (v) => v == null || v.isEmpty ? "Category is required" : null,
                    onSaved: (v) => _selectedCategory = v,
                  ),
                  const SizedBox(height: 15),

                  // Condition
                  DropdownButtonFormField<String>(
                    value: _selectedCondition,
                    decoration: _getInputDecoration("Condition", Icons.star_border),
                    hint: const Text("Select Item Condition"),
                    items: _conditions.map((String value) {
                      return DropdownMenuItem<String>(
                        value: value,
                        child: Text(value),
                      );
                    }).toList(),
                    onChanged: (String? newValue) {
                      setState(() {
                        _selectedCondition = newValue;
                      });
                    },
                    validator: (v) => v == null || v.isEmpty ? "Condition is required" : null,
                    onSaved: (v) => _selectedCondition = v,
                  ),
                  const SizedBox(height: 30),

                  // Buton Update
                  SizedBox(
                    height: 50,
                    child: _isLoading
                        ? const Center(child: CircularProgressIndicator(color: Colors.deepPurple))
                        : ElevatedButton(
                      onPressed: _submit,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.deepPurple,
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                        elevation: 5,
                      ),
                      child: const Text("Update Item", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildImagePreview() {
    if (_webImageBytes != null) {
      // Caz: Web - Imagine nouă selectată
      return Image.memory(_webImageBytes!, fit: BoxFit.cover);
    } else if (_pickedImage != null && !kIsWeb) {
      // Caz: Mobile - Imagine nouă selectată
      return Image.file(File(_pickedImage!.path), fit: BoxFit.cover);
    } else if (_imageUrl != null && _imageUrl!.isNotEmpty) {
      // Caz: Imagine existentă de la server
      return Image.network(
        _imageUrl!,
        fit: BoxFit.cover,
        errorBuilder: (ctx, err, stack) => const Center(child: Icon(Icons.broken_image, size: 50, color: Colors.grey)),
      );
    } else {
      // Caz: Nicio imagine
      return const Center(child: Icon(Icons.image, size: 80, color: Colors.grey));
    }
  }
}