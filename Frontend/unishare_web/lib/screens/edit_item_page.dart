import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:http/http.dart' as http;
import '../services/api_service.dart';
import '../services/chat_service.dart';

class EditItemPage extends StatefulWidget {
  final Map<String, dynamic> item;

  const EditItemPage({super.key, required this.item});

  @override
  State<EditItemPage> createState() => _EditItemPageState();
}

class _EditItemPageState extends State<EditItemPage> {
  final _formKey = GlobalKey<FormState>();

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

  XFile? _pickedImage;
  Uint8List? _webImageBytes;

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
      String? blobName;

      // 1. Upload document if a new image was selected
      if (_pickedImage != null) {
        print('📤 Uploading new document...');
        
        // Get file bytes
        final fileBytes = await _pickedImage!.readAsBytes();
        final fileName = _pickedImage!.name;
        
        // Determine MIME type
        final extension = fileName.toLowerCase().split('.').last;
        String mimeType;
        switch (extension) {
          case 'jpg':
          case 'jpeg':
            mimeType = 'image/jpeg';
            break;
          case 'png':
            mimeType = 'image/png';
            break;
          case 'gif':
            mimeType = 'image/gif';
            break;
          case 'webp':
            mimeType = 'image/webp';
            break;
          default:
            mimeType = 'image/jpeg';
        }

        // Step 1a: Get SAS URL and blobName from backend
        final sasData = await ChatService.retrieveSasUrl(fileName, mimeType);
        if (sasData == null) {
          throw Exception('Failed to retrieve upload URL from server');
        }

        final uploadUrl = sasData['uploadUrl'] as String;
        blobName = sasData['blobName'] as String;

        print('📤 Uploading to blob storage: $blobName');

        // Step 1b: Upload directly to blob storage
        final uploadUri = Uri.parse(uploadUrl);
        final uploadResponse = await http.put(
          uploadUri,
          headers: {
            'x-ms-blob-type': 'BlockBlob',
            'Content-Type': mimeType,
          },
          body: fileBytes,
        );

        if (uploadResponse.statusCode != 201 && uploadResponse.statusCode != 200) {
          throw Exception('Failed to upload document to blob storage: ${uploadResponse.statusCode}');
        }

        print('✅ Document uploaded successfully to blob storage');
      }

      // 2. Update item using PATCH endpoint
      print('📝 Updating item...');
      final itemId = widget.item['id']?.toString();
      if (itemId == null) {
        throw Exception('Item ID not found');
      }

      final result = await ApiService.patchItem(
        itemId: itemId,
        itemName: _name,
        description: _description,
        category: _selectedCategory,
        condition: _selectedCondition,
        blobName: blobName, // Pass blobName (can be null if no new image)
      );

      setState(() => _isLoading = false);

      if (mounted) {
        if (result['success'] == true) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text("Item updated successfully!"),
              backgroundColor: Colors.green,
            ),
          );
          Navigator.pop(context, true);
        } else {
          final errors = result['errors'] as Map<String, dynamic>?;
          String errorMessage = 'Failed to update item';
          if (errors != null) {
            errorMessage = errors.values.join(', ');
          }
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(errorMessage),
              backgroundColor: Colors.red,
            ),
          );
        }
      }
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text("Error: $e"),
            backgroundColor: Colors.red,
          ),
        );
      }
      print('❌ Error updating item: $e');
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

                  // Image Picker Section
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

                  // Update Button
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
      return Image.memory(_webImageBytes!, fit: BoxFit.cover);
    } else if (_pickedImage != null && !kIsWeb) {
      return Image.file(File(_pickedImage!.path), fit: BoxFit.cover);
    } else if (_imageUrl != null && _imageUrl!.isNotEmpty) {
      return Image.network(
        _imageUrl!,
        fit: BoxFit.cover,
        errorBuilder: (ctx, err, stack) => const Center(child: Icon(Icons.broken_image, size: 50, color: Colors.grey)),
      );
    } else {
      return const Center(child: Icon(Icons.image, size: 80, color: Colors.grey));
    }
  }
}
