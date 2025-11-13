import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:unishare_web/screens/verify_email_page.dart';
import '../providers/auth_provider.dart';
import 'login_page.dart';

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});

  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameCtrl = TextEditingController();
  final _lastNameCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _userNameCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  bool _loading = false;

  @override
  void initState() {
    super.initState();

    // ---------------- LISTENERI PENTRU ȘTERGEREA ERORILOR ----------------
    _emailCtrl.addListener(() => _clearFieldError('email'));
    _userNameCtrl.addListener(() => _clearFieldError('userName'));
  }

  void _clearFieldError(String field) {
    final auth = context.read<AuthProvider>();
    if (auth.fieldErrors.containsKey(field)) {
      auth.fieldErrors.remove(field);
      // Forțează revalidarea form-ului ca să dispară mesajul sub câmp
      if (_formKey.currentState != null) _formKey.currentState!.validate();
      // Notify widget să se re-renderizeze
      setState(() {});
    }
  }

  Future<void> _register() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _loading = true);
    final auth = context.read<AuthProvider>();
    final success = await auth.register(
      firstName: _firstNameCtrl.text.trim(),
      lastName: _lastNameCtrl.text.trim(),
      email: _emailCtrl.text.trim(),
      userName: _userNameCtrl.text.trim(),
      password: _passwordCtrl.text.trim(),
    );
    setState(() => _loading = false);

    _formKey.currentState!.validate(); // revalidate pentru a arăta erorile

    if (!mounted) return;

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Registration successful! Check your email for verification code.')),
      );

      // Navigăm către VerifyEmailPage
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(
          builder: (_) => VerifyEmailPage(email: _emailCtrl.text.trim()),
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Registration failed.')),
      );
    }
  }


  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    return Scaffold(
      appBar: AppBar(title: const Text('Register')),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Form(
          key: _formKey,
          child: ListView(
            children: [
              TextFormField(
                controller: _firstNameCtrl,
                decoration: const InputDecoration(labelText: 'First Name'),
                validator: (v) => v!.isEmpty ? 'Enter your first name' : null,
              ),
              TextFormField(
                controller: _lastNameCtrl,
                decoration: const InputDecoration(labelText: 'Last Name'),
                validator: (v) => v!.isEmpty ? 'Enter your last name' : null,
              ),
              TextFormField(
                controller: _emailCtrl,
                decoration: const InputDecoration(labelText: 'Email'),
                validator: (v) {
                  if (!v!.contains('@')) return 'Enter a valid email';
                  if (auth.fieldErrors.containsKey('email')) return auth.fieldErrors['email'];
                  return null;
                },
              ),
              TextFormField(
                controller: _userNameCtrl,
                decoration: const InputDecoration(labelText: 'Username'),
                validator: (v) {
                  if (v!.isEmpty) return 'Enter a username';
                  if (auth.fieldErrors.containsKey('userName')) return auth.fieldErrors['userName'];
                  return null;
                },
              ),
              TextFormField(
                controller: _passwordCtrl,
                decoration: const InputDecoration(labelText: 'Password'),
                obscureText: true,
                validator: (v) {
                  if (v == null || v.isEmpty) return 'Enter a password';
                  if (v.length < 6) return 'Minimum 6 characters';
                  if (!RegExp(r'[0-9]').hasMatch(v)) return 'Must contain at least 1 number';
                  if (!RegExp(r'[!@#$%^&*(),.?":{}|<>]').hasMatch(v)) return 'Must contain at least 1 special character';
                  return null;
                },
              ),
              const SizedBox(height: 20),
              ElevatedButton(
                onPressed: _loading ? null : _register,
                child: _loading
                    ? const CircularProgressIndicator(color: Colors.white)
                    : const Text('Register'),
              ),
              TextButton(
                onPressed: () {
                  Navigator.pushReplacement(
                    context,
                    MaterialPageRoute(builder: (_) => const LoginPage()),
                  );
                },
                child: const Text('Already have an account? Login'),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  void dispose() {
    _firstNameCtrl.dispose();
    _lastNameCtrl.dispose();
    _emailCtrl.dispose();
    _userNameCtrl.dispose();
    _passwordCtrl.dispose();
    super.dispose();
  }
}
