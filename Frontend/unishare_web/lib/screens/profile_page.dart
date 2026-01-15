import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../services/api_service.dart';
import '../services/secure_storage_service.dart';
import 'verify_email_page.dart';
import 'edit_profile_page.dart';
import 'conversations_page.dart'; // Import necesar

class ProfilePage extends StatefulWidget {
  const ProfilePage({super.key});

  @override
  State<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  bool? emailVerified;
  bool isLoading = false;
  bool isLoadingUserData = true;
  String? firstName;
  String? lastName;
  String? universityName;
  bool _isSubmittingModeratorRequest = false;

  Future<void> loadUserData() async {
    setState(() => isLoadingUserData = true);
    final auth = context.read<AuthProvider>();

    String? token = auth.token;
    if (token == null) {
      token = await SecureStorageService.getAccessToken();
      if (token == null) {
        if (mounted) setState(() => isLoadingUserData = false);
        return;
      }
    }

    final userId = ApiService.getUserIdFromToken(token);

    if (userId != null) {
      try {
        final userData = await ApiService.getUser(userId).timeout(
          const Duration(seconds: 10),
          onTimeout: () => null,
        );

        if (userData != null && mounted) {
          setState(() {
            firstName = userData['firstName'];
            lastName = userData['lastName'];
            universityName = userData['universityName'];
            isLoadingUserData = false;
          });
        } else if (mounted) {
          setState(() => isLoadingUserData = false);
        }
      } catch (e) {
        if (mounted) setState(() => isLoadingUserData = false);
      }
    } else {
      if (mounted) setState(() => isLoadingUserData = false);
    }
  }

  Future<void> checkEmailVerified() async {
    setState(() => isLoading = true);
    final auth = context.read<AuthProvider>();
    String? token = auth.token;

    if (token == null) {
      token = await SecureStorageService.getAccessToken();
    }

    bool? result = auth.emailVerified;

    if (result == null) {
      try {
        result = ApiService.getEmailVerifiedFromToken(token);
      } catch (e) {
        result = null;
      }
    }

    if (result == null && token != null && token.isNotEmpty) {
      try {
        result = await ApiService.getEmailVerifiedStatus(token);
      } catch (e) {
        result = null;
      }
    }

    setState(() {
      emailVerified = result;
      isLoading = false;
    });
  }

  @override
  void initState() {
    super.initState();
    checkEmailVerified();
    loadUserData();
  }

  void _logout() {
    final auth = context.read<AuthProvider>();
    auth.logout();
    Navigator.pushNamedAndRemoveUntil(context, '/login', (route) => false);
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final email = auth.currentUserEmail ?? "unknown@unishare.com";
    final token = auth.token;
    final userId = token != null ? ApiService.getUserIdFromToken(token) : null;

    final roles = ApiService.getUserRolesFromToken(token).map((r) => r.toLowerCase()).toList();
    final bool isAdmin = roles.contains('admin');
    final bool isModerator = roles.contains('moderator');
    final accountTypeLabel = isAdmin ? 'Admin' : (isModerator ? 'Moderator' : 'Standard User');

    bool? displayedVerified = emailVerified ?? auth.emailVerified;
    if (displayedVerified == null && token != null && token.isNotEmpty) {
      try {
        displayedVerified = ApiService.getEmailVerifiedFromToken(token);
      } catch (e) { }
    }

    // --- AICI ESTE MODIFICAREA PRINCIPALĂ: Returnăm Scaffold cu AppBar ---
    return Scaffold(
      appBar: AppBar(
        title: const Text("My Profile", style: TextStyle(color: Colors.black)),
        backgroundColor: Colors.white,
        elevation: 1,
        // Optional: Buton pentru Drawer
        // leading: IconButton(
        //   icon: const Icon(Icons.menu, color: Colors.black),
        //   onPressed: () => Scaffold.of(context).openDrawer(),
        // ),
        actions: [
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
            onPressed: _logout,
          ),
        ],
      ),
      body: isLoadingUserData
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          children: [
            const SizedBox(height: 20),
            const CircleAvatar(
              radius: 60,
              backgroundImage: NetworkImage(
                "https://cdn-icons-png.flaticon.com/512/149/149071.png",
              ),
              backgroundColor: Colors.transparent,
            ),
            const SizedBox(height: 20),
            Text(
              firstName != null && lastName != null
                  ? "$firstName $lastName"
                  : "UniShare User",
              style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold),
            ),
            Text(email, style: const TextStyle(color: Colors.grey, fontSize: 16)),
            const SizedBox(height: 20),

            // --- Email Verified Section ---
            isLoading
                ? const CircularProgressIndicator()
                : Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  displayedVerified == true ? "Email Verified ✅" : "Email Not Verified ❌",
                  style: TextStyle(
                    color: displayedVerified == true ? Colors.green : Colors.red,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                if (displayedVerified != true)
                  TextButton(
                    onPressed: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (_) => VerifyEmailPage(email: email, userId: userId),
                        ),
                      ).then((_) => checkEmailVerified());
                    },
                    child: const Text("Verify"),
                  ),
              ],
            ),
            const SizedBox(height: 30),
            const Divider(thickness: 1),
            const SizedBox(height: 10),

            // --- Rest of profile info ---
            ListTile(
              leading: const Icon(Icons.email_outlined),
              title: const Text("Email"),
              subtitle: Text(email),
            ),
            if (universityName != null)
              ListTile(
                leading: const Icon(Icons.school_outlined),
                title: const Text("University"),
                subtitle: Text(universityName!),
              ),
            ListTile(
              leading: const Icon(Icons.account_circle_outlined),
              title: const Text("Account Type"),
              subtitle: Text(accountTypeLabel),
            ),
            ListTile(
              leading: const Icon(Icons.calendar_today_outlined),
              title: const Text("Member Since"),
              subtitle: const Text("November 2025"),
            ),
            const SizedBox(height: 30),

            // Butoane: Edit Profile și Reset Password
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                ElevatedButton.icon(
                  onPressed: (userId != null && !isLoadingUserData && firstName != null && lastName != null)
                      ? () async {
                    final result = await Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (_) => EditProfilePage(
                          userId: userId,
                          currentFirstName: firstName!,
                          currentLastName: lastName!,
                          currentEmail: email,
                          currentUniversity: universityName,
                        ),
                      ),
                    );
                    if (result == true) {
                      await loadUserData();
                    }
                  }
                      : null,
                  icon: const Icon(Icons.edit),
                  label: const Text("Edit Profile"),
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(10),
                    ),
                  ),
                ),
                const SizedBox(width: 16),
                ElevatedButton.icon(
                  onPressed: () {
                    Navigator.pushNamed(context, '/forgot-password');
                  },
                  icon: const Icon(Icons.lock_reset),
                  label: const Text("Reset Password"),
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(10),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            if (!isAdmin && !isModerator)
              ElevatedButton.icon(
                onPressed: (userId != null && displayedVerified == true && !_isSubmittingModeratorRequest)
                    ? () => _showModeratorRequestDialog(userId)
                    : null,
                icon: _isSubmittingModeratorRequest
                    ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.black))
                    : const Icon(Icons.how_to_reg),
                label: const Text("Request Moderator"),
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.purple,
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                  foregroundColor: Colors.white,
                ),
              ),
          ],
        ),
      ),
    );
  }

  Future<void> _showModeratorRequestDialog(String userId) async {
    final _formKey = GlobalKey<FormState>();
    final _reasonController = TextEditingController();

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: const Text('Request Moderator Access'),
          content: SizedBox(
            width: 500,
            child: Form(
              key: _formKey,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text('Tell us why you should be a moderator (min 20 characters):'),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: _reasonController,
                    maxLines: 5,
                    maxLength: 1000,
                    decoration: const InputDecoration(border: OutlineInputBorder()),
                    validator: (v) {
                      if (v == null || v.trim().length < 20) return 'Please provide at least 20 characters';
                      return null;
                    },
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(onPressed: () => Navigator.of(ctx).pop(false), child: const Text('Cancel')),
            ElevatedButton(
              onPressed: () async {
                if (!_formKey.currentState!.validate()) return;
                Navigator.of(ctx).pop(true);
              },
              child: const Text('Submit'),
            ),
          ],
        );
      },
    );

    if (result == true) {
      final reason = _reasonController.text.trim();
      setState(() => _isSubmittingModeratorRequest = true);
      try {
        final resp = await ApiService.createModeratorRequest(userId: userId, reason: reason);
        if (!mounted) return;
        if (resp['success'] == true) {
          // AICI ERA EROAREA: 'aconst' -> 'const'
          ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Moderator request submitted successfully'), backgroundColor: Colors.green));
        } else {
          final msg = resp['message'] ?? 'Failed to submit moderator request';
          ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg), backgroundColor: Colors.red));
        }
      } catch (e) {
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red));
      } finally {
        if (mounted) setState(() => _isSubmittingModeratorRequest = false);
      }
    }
  }
}