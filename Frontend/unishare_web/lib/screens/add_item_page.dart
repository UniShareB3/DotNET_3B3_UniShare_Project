import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import '../services/api_service.dart';
import '../services/chat_service.dart';
import '../services/secure_storage_service.dart'; // Necesar pentru a obține userId

class AddItemPage extends StatefulWidget {
  const AddItemPage({super.key});

  @override
  State<AddItemPage> createState() => _AddItemPageState();
}

class _AddItemPageState extends State<AddItemPage> {
  final _formKey = GlobalKey<FormState>();

  final List<String> _categories = ['Others', 'Books', 'Electronics', 'Kitchen', 'Clothing', 'Accessories'];
  final List<String> _conditions = ['New', 'Excellent', 'Good', 'Fair', 'Poor'];

  // Stare pentru a stoca datele formularului
  String _name = "";
  String _description = "";
  String? _selectedCategory;
  String? _selectedCondition;
  String? _imageUrl; // URL-ul final al imaginii (după upload)

  // Stare pentru imaginea selectată local
  XFile? _pickedImage;
  Uint8List? _pickedImageBytes;

  bool _isUploading = false;
  bool _isLoading = false;

  // Funcție utilitară pentru a aplica stilul de input
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

  // 1. Selectare Imagine din Galerie
  Future<void> _pickImage() async {
    final ImagePicker picker = ImagePicker();
    try {
      final XFile? image = await picker.pickImage(source: ImageSource.gallery);
      if (image != null) {
        final bytes = await image.readAsBytes();
        setState(() {
          _pickedImage = image;
          _pickedImageBytes = bytes;
        });

        // Declanșăm upload-ul automat după selectare
        _uploadImage(image, bytes);
      }
    } catch (e) {
      print('Error picking image: $e');
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Failed to pick image')),
      );
    }
  }

  // 2. Upload Imagine folosind ChatService.uploadDocument
  // Această metodă gestionează SAS, Upload-ul propriu-zis și Confirmarea (care returnează URL-ul public)
  Future<void> _uploadImage(XFile imageFile, Uint8List bytes) async {
    setState(() => _isUploading = true);

    try {
      // Obținem ID-ul utilizatorului curent pentru a-l folosi ca "receiver" (self-upload)
      // Acest lucru este necesar pentru a satisface cerințele backend-ului
      final token = await SecureStorageService.getAccessToken();
      final userId = ApiService.getUserIdFromToken(token ?? '');

      if (userId == null) {
        throw Exception("You must be logged in to upload images.");
      }

      // Folosim metoda completă din ChatService
      // Aceasta va returna un Map care conține 'documentUrl' valid
      final result = await ChatService.uploadDocument(
          bytes.toList(), // Convertim la List<int>
          imageFile.name,
          userId // Trimitem ID-ul nostru pentru a confirma upload-ul
      );

      if (result != null && result['documentUrl'] != null) {
        setState(() {
          _imageUrl = result['documentUrl']; // Salvăm URL-ul public primit de la server
        });

        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Image uploaded successfully!')),
          );
        }
      } else {
        throw Exception("Upload succeeded but no URL was returned.");
      }

    } catch (e) {
      print("Upload error: $e");
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error uploading image: $e')),
        );
      }
      // Resetăm starea imaginii în caz de eșec pentru a permite reîncercarea
      setState(() {
        _pickedImage = null;
        _pickedImageBytes = null;
        _imageUrl = null;
      });
    } finally {
      if (mounted) {
        setState(() => _isUploading = false);
      }
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    _formKey.currentState!.save();

    // Verificare Dropdown-uri
    if (_selectedCategory == null || _selectedCondition == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text("Please select a Category and Condition")),
      );
      return;
    }

    // Nu permitem trimiterea dacă upload-ul e în curs
    if (_isUploading) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text("Please wait for image upload to finish.")),
      );
      return;
    }

    setState(() => _isLoading = true);

    final result = await ApiService.postItem(
      name: _name,
      description: _description,
      category: _selectedCategory!,
      condition: _selectedCondition!,
      imageUrl: _imageUrl, // Trimitem URL-ul obținut din procesul de upload
    );

    setState(() => _isLoading = false);

    if (result == true) {
      if (mounted) Navigator.pop(context, true); // return success
    } else {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text("Failed to create item")),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final double maxFormWidth = 600.0;

    return Scaffold(
      appBar: AppBar(
        title: const Text("List New Item"),
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
                    'Item Details',
                    style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Colors.deepPurple),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 30),

                  // --- Image Picker Section ---
                  Center(
                    child: Column(
                      children: [
                        Container(
                          height: 250,
                          width: double.infinity,
                          decoration: BoxDecoration(
                            color: Colors.grey.shade100,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(color: Colors.grey.shade300),
                          ),
                          clipBehavior: Clip.antiAlias,
                          child: Stack(
                            fit: StackFit.expand,
                            children: [
                              _buildImagePreview(),
                              if (_isUploading)
                                Container(
                                  color: Colors.black45,
                                  child: const Center(
                                    child: CircularProgressIndicator(color: Colors.white),
                                  ),
                                ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 12),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            ElevatedButton.icon(
                              onPressed: _isUploading ? null : _pickImage,
                              icon: const Icon(Icons.cloud_upload),
                              label: const Text('Select & Upload Image'),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.deepPurple.shade50,
                                foregroundColor: Colors.deepPurple,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 30),

                  // Name
                  TextFormField(
                    decoration: _getInputDecoration("Item Name", Icons.label_outline),
                    validator: (v) => v!.isEmpty ? "Item name is required" : null,
                    onSaved: (v) => _name = v!,
                  ),
                  const SizedBox(height: 15),

                  // Description
                  TextFormField(
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

                  // Submit Button
                  SizedBox(
                    height: 50,
                    child: _isLoading
                        ? const Center(child: CircularProgressIndicator(color: Colors.deepPurple))
                        : ElevatedButton(
                      onPressed: _isUploading ? null : _submit,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.deepPurple,
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                        elevation: 5,
                      ),
                      child: const Text("Create Item", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
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
    if (_pickedImageBytes != null) {
      // Imaginea selectată local
      return Image.memory(_pickedImageBytes!, fit: BoxFit.cover);
    } else {
      // Placeholder
      return const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.image, size: 80, color: Colors.grey),
            Text("No image selected", style: TextStyle(color: Colors.grey)),
          ],
        ),
      );
    }
  }
}