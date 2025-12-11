import 'dart:async';
import 'package:flutter/material.dart';
import '../services/api_service.dart';

class VerifyEmailPage extends StatefulWidget {
  final String email;
  final String userId;
  const VerifyEmailPage({super.key, required this.email,required this.userId});

  @override
  State<VerifyEmailPage> createState() => _VerifyEmailPageState();
}

class _VerifyEmailPageState extends State<VerifyEmailPage> {
  final codeController = TextEditingController();
  bool isLoading = false;
  int verificationNr=0;
  bool canResend = true;
  int resendTimer = 60;
  Timer? timer;

  @override
  void initState() {
    super.initState();
    _sendCode();
    startResendTimer();
  }

  void startResendTimer() {
    setState(() {
      canResend = false;
      resendTimer = 60;
    });

    timer?.cancel();
    timer = Timer.periodic(const Duration(seconds: 1), (t) {
      if (resendTimer == 0) {
        t.cancel();
        setState(() => canResend = true);
      } else {
        setState(() => resendTimer--);
      }
    });
  }

  @override
  void dispose() {
    codeController.dispose();
    timer?.cancel();
    super.dispose();
  }

  Future<void> _verify() async {
    setState(() => isLoading = true);
    final success = await ApiService.confirmEmail(widget.userId, codeController.text);
    setState(() => isLoading = false);

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Email verified successfully!')),
      );
      Navigator.pop(context, true); // închide pagina și notifică că s-a verificat
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Verification failed. Try again.')),
      );
    }
  }

  Future<void> _sendCode() async {
    setState(() => isLoading = true);
    final success = await ApiService.sendVerificationCode(widget.userId);
    setState(() => isLoading = false);

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Verification code sent!')),
      );
      startResendTimer();
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Failed to resend code. Try again.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Verify Email'),
        actions: [
          IconButton(
            icon: const Icon(Icons.close),
            onPressed: () => Navigator.pop(context),
          ),
        ],
      ),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text('Enter the verification code sent to ${widget.email}'),
              const SizedBox(height: 10),
              TextField(
                controller: codeController,
                decoration: const InputDecoration(labelText: 'Code'),
              ),
              const SizedBox(height: 16),
              isLoading
                  ? const CircularProgressIndicator()
                  : ElevatedButton(
                onPressed: _verify,
                child: const Text('Verify'),
              ),
              const SizedBox(height: 16),
              TextButton(
                onPressed: canResend ? _sendCode : null,
                child: Text(
                  canResend ? 'Resend Code' : 'Resend in $resendTimer s',
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
