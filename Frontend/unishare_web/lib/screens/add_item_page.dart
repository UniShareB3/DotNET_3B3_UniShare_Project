import 'package:flutter/material.dart';
import '../services/api_service.dart';

class AddItemPage extends StatefulWidget {
  const AddItemPage({super.key});

  @override
  State<AddItemPage> createState() => _AddItemPageState();
}

class _AddItemPageState extends State<AddItemPage> {
  final _formKey = GlobalKey<FormState>();

  // ACTUALIZAT: Listele corespund acum cu valorile valide din backend
  final List<String> _categories = ['Others', 'Books', 'Electronics', 'Kitchen', 'Clothing', 'Accessories'];
  final List<String> _conditions = ['Excellent', 'Good', 'Fair', 'Poor'];

  // Stare pentru a stoca datele formularului
  String _name = "";
  String _description = "";
  String? _selectedCategory; // Nullable pentru Dropdown
  String? _selectedCondition; // Nullable pentru Dropdown
  String? _imageUrl; // NOU: Stare pentru URL imagine

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

  // Funcție de validare opțională pentru URL
  String? _validateImageUrl(String? v) {
    if (v == null || v.isEmpty) return null; // Nu e obligatoriu

    // Regex simplu pentru URL
    final urlRegex = RegExp(r'^https?:\/\/');
    if (!urlRegex.hasMatch(v)) {
      return "Please enter a valid URL (starts with http:// or https://)";
    }
    return null;
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    _formKey.currentState!.save();

    // Verificare suplimentară pentru Dropdown-uri
    if (_selectedCategory == null || _selectedCondition == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text("Please select a Category and Condition")),
      );
      return;
    }

    setState(() => _isLoading = true);

    final result = await ApiService.postItem(
      name: _name,
      description: _description,
      category: _selectedCategory!, // Acum nu mai este null
      condition: _selectedCondition!, // Acum nu mai este null
      imageUrl: _imageUrl, // NOU: Trimitem URL-ul
    );

    setState(() => _isLoading = false);

    if (result == true) {
      Navigator.pop(context, true); // return success
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text("Failed to create item")),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final double maxFormWidth = 600.0; // Lățime mai mare pentru un formular lung

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
                  // 1. Titlu
                  const Text(
                    'Item Details',
                    style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Colors.deepPurple),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 30),

                  // 2. Item Name
                  TextFormField(
                    decoration: _getInputDecoration("Item Name", Icons.label_outline),
                    validator: (v) => v!.isEmpty ? "Item name is required" : null,
                    onSaved: (v) => _name = v!,
                  ),
                  const SizedBox(height: 15),

                  // 3. Description
                  TextFormField(
                    decoration: _getInputDecoration("Description", Icons.description_outlined).copyWith(
                      alignLabelWithHint: true, // Pentru a alinia eticheta sus la multiline
                    ),
                    maxLines: 4,
                    validator: (v) => v!.isEmpty ? "Description is required" : null,
                    onSaved: (v) => _description = v!,
                  ),
                  const SizedBox(height: 15),

                  // 4. Category (Dropdown)
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

                  // 5. Condition (Dropdown)
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
                  const SizedBox(height: 15),

                  // NOU: 6. Image URL (Opțional)
                  TextFormField(
                    decoration: _getInputDecoration("Image URL (Optional)", Icons.link_outlined),
                    validator: _validateImageUrl,
                    onSaved: (v) => _imageUrl = v!.isEmpty ? null : v, // Setează null dacă este gol
                  ),
                  const SizedBox(height: 30),

                  // 7. Create Item Button
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
}