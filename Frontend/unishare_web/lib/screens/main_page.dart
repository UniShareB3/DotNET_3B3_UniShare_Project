import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../services/api_service.dart';
import '../services/secure_storage_service.dart';
import '../services/chat_service.dart';
import 'home_page.dart';
import 'dashboard_page.dart';
import 'profile_page.dart';
import 'login_page.dart';
import 'moderator_reports_page.dart';
import 'admin_moderator_requests_page.dart';
import 'conversations_page.dart';

class MainPage extends StatefulWidget {
  const MainPage({super.key});

  @override
  State<MainPage> createState() => _MainPageState();
}

class _MainPageState extends State<MainPage> {
  int _selectedIndex = 0;
  bool _isAdminOrModerator = false;
  bool _isAdmin = false;
  bool _hasUnreadMessages = false;

  @override
  void initState() {
    super.initState();
    _checkUserRole();
    _setupMessageListener();
    _checkForUnreadMessages();
  }

  @override
  void dispose() {
    ChatService.removeMessageListener(_onNewMessage);
    super.dispose();
  }

  void _setupMessageListener() {
    ChatService.getConnection();
    ChatService.addMessageListener(_onNewMessage);
  }

  void _onNewMessage(Map<String, dynamic> message) {
    if (mounted) {
      setState(() {
        _hasUnreadMessages = true;
      });
    }
  }

  Future<void> _checkForUnreadMessages() async {
    try {
      final conversations = await ChatService.getConversations();
      if (mounted && conversations.isNotEmpty) {
        // Logică simplificată: dacă există conversații, presupunem că pot fi mesaje
        // Într-o implementare reală, backend-ul ar trebui să returneze un flag 'unreadCount'
      }
    } catch (e) {
      print('Error checking unread messages: $e');
    }
  }

  Future<void> _checkUserRole() async {
    final token = await SecureStorageService.getAccessToken();
    setState(() {
      _isAdminOrModerator = ApiService.isAdminOrModerator(token);
      _isAdmin = ApiService.isAdmin(token);
    });
  }

  void _onItemTapped(int index) {
    setState(() => _selectedIndex = index);
    // Dacă utilizatorul merge pe profil (unde poate accesa mesaje), putem ascunde badge-ul
    // sau îl lăsăm până intră efectiv în mesaje.
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
    final userEmail = context.watch<AuthProvider>().currentUserEmail ?? "Guest";

    // Paginile care vor fi afișate. Fiecare își gestionează acum propriul AppBar.
    final pages = [
      const HomePage(),
      const DashboardPage(),
      const ProfilePage(), // Asigură-te că ProfilePage are acum un Scaffold
    ];

    return Scaffold(
      // --- AM SCOS APPBAR-UL DE AICI PENTRU A EVITA DUPLICAREA ---

      body: pages[_selectedIndex],

      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _selectedIndex,
        onTap: _onItemTapped,
        selectedItemColor: Theme.of(context).primaryColor,
        unselectedItemColor: Colors.grey,
        type: BottomNavigationBarType.fixed,
        items: [
          const BottomNavigationBarItem(icon: Icon(Icons.home), label: "Home"),
          const BottomNavigationBarItem(icon: Icon(Icons.dashboard), label: "Dashboard"),
          BottomNavigationBarItem(
            icon: Stack(
              children: [
                const Icon(Icons.person),
                if (_hasUnreadMessages)
                  Positioned(
                    right: 0,
                    top: 0,
                    child: Container(
                      padding: const EdgeInsets.all(2),
                      decoration: const BoxDecoration(
                        color: Colors.red,
                        shape: BoxShape.circle,
                      ),
                      constraints: const BoxConstraints(
                        minWidth: 8,
                        minHeight: 8,
                      ),
                    ),
                  ),
              ],
            ),
            label: "Profile",
          ),
        ],
      ),

      // Drawer-ul rămâne aici. Poate fi deschis prin swipe de la stânga
      // sau adăugând un buton dedicat în AppBar-ul paginilor copil.
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
              onTap: () {
                Navigator.pop(context);
                _onItemTapped(0);
              },
            ),
            ListTile(
              leading: const Icon(Icons.dashboard),
              title: const Text("Dashboard"),
              onTap: () {
                Navigator.pop(context);
                _onItemTapped(1);
              },
            ),
            ListTile(
              leading: Stack(
                children: [
                  const Icon(Icons.message),
                  if (_hasUnreadMessages)
                    Positioned(
                      right: 0,
                      top: 0,
                      child: Container(
                        padding: const EdgeInsets.all(3),
                        decoration: const BoxDecoration(
                          color: Colors.red,
                          shape: BoxShape.circle,
                        ),
                        constraints: const BoxConstraints(
                          minWidth: 10,
                          minHeight: 10,
                        ),
                      ),
                    ),
                ],
              ),
              title: const Text("Messages"),
              onTap: () {
                setState(() {
                  _hasUnreadMessages = false;
                });
                Navigator.pop(context);
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (context) => const ConversationsPage(),
                  ),
                );
              },
            ),
            ListTile(
              leading: const Icon(Icons.person),
              title: const Text("Profile"),
              onTap: () {
                Navigator.pop(context);
                _onItemTapped(2);
              },
            ),
            if (_isAdminOrModerator) ...[
              const Divider(),
              ListTile(
                leading: const Icon(Icons.flag, color: Colors.red),
                title: const Text("Moderator Reports"),
                onTap: () {
                  Navigator.pop(context);
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