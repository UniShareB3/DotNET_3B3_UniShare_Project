import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../services/api_service.dart';
import '../services/secure_storage_service.dart';
import 'home_page.dart';
import 'dashboard_page.dart';
import 'profile_page.dart';
import 'login_page.dart';
import 'moderator_reports_page.dart';
import 'admin_moderator_requests_page.dart';

class MainPage extends StatefulWidget {
  const MainPage({super.key});

  @override
  State<MainPage> createState() => _MainPageState();
}

class _MainPageState extends State<MainPage> {
  int _selectedIndex = 0;
  bool _isAdminOrModerator = false;
  bool _isAdmin = false;

  @override
  void initState() {
    super.initState();
    _checkUserRole();
  }

  Future<void> _checkUserRole() async {
    final token = await SecureStorageService.getAccessToken();
    setState(() {
      _isAdminOrModerator = ApiService.isAdminOrModerator(token);
      _isAdmin = ApiService.isAdmin(token);
    });
  }

  void _onItemTapped(int index) => setState(() => _selectedIndex = index);

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
    final userEmail = context.watch<AuthProvider>().currentUserEmail ?? "Guest";

    final pages = [
      const HomePage(),
      const DashboardPage(),
      const ProfilePage(),
    ];

    return Scaffold(
      appBar: AppBar(
        title: const Text("UniShare"),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: _logout,
          ),
        ],
      ),
      body: pages[_selectedIndex],
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _selectedIndex,
        onTap: _onItemTapped,
        selectedItemColor: Theme.of(context).primaryColor,
        unselectedItemColor: Colors.grey,
        items: const [
          BottomNavigationBarItem(icon: Icon(Icons.home), label: "Home"),
          BottomNavigationBarItem(icon: Icon(Icons.dashboard), label: "Dashboard"),
          BottomNavigationBarItem(icon: Icon(Icons.person), label: "Profile"),
        ],
      ),
      drawer: Drawer(
        child: ListView(
          padding: EdgeInsets.zero,
          children: [
            UserAccountsDrawerHeader(
              accountName: const Text("UniShare User"),
              accountEmail: Text(userEmail),
              currentAccountPicture: const CircleAvatar(
                backgroundColor: Colors.white,
                child: Icon(Icons.person, size: 40, color: Colors.blue),
              ),
            ),
            ListTile(
              leading: const Icon(Icons.home),
              title: const Text("Home"),
              onTap: () => _onItemTapped(0),
            ),
            ListTile(
              leading: const Icon(Icons.dashboard),
              title: const Text("Dashboard"),
              onTap: () => _onItemTapped(1),
            ),
            ListTile(
              leading: const Icon(Icons.person),
              title: const Text("Profile"),
              onTap: () => _onItemTapped(2),
            ),
            if (_isAdminOrModerator) ...[
              const Divider(),
              ListTile(
                leading: const Icon(Icons.flag, color: Colors.red),
                title: const Text("Moderator Reports"),
                onTap: () {
                  Navigator.pop(context); // Close drawer
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (context) => const ModeratorReportsPage(),
                    ),
                  );
                },
              ),
            ],
            if (_isAdmin) ...[
              ListTile(
                leading: const Icon(Icons.admin_panel_settings, color: Colors.blue),
                title: const Text('Admin: Moderator Requests'),
                onTap: () {
                  Navigator.pop(context);
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => const AdminModeratorRequestsPage(),
                    ),
                  );
                },
              ),
            ],
            const Divider(),
            ListTile(
              leading: const Icon(Icons.logout),
              title: const Text("Logout"),
              onTap: _logout,
            ),
          ],
        ),
      ),
    );
  }
}
