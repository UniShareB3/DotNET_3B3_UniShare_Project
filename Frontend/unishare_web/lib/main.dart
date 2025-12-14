import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:provider/provider.dart';
import 'package:unishare_web/screens/main_page.dart';
import 'providers/auth_provider.dart';
import 'screens/login_page.dart';
import 'screens/register_page.dart';
import 'screens/home_page.dart';
import 'screens/product_page.dart';
import 'screens/forgot_password_page.dart';
import 'screens/reset_password_page.dart';

final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  
  final authProvider = AuthProvider();
  await authProvider.tryAutoLogin(); // încercă să încarce token-ul

  runApp(
    ChangeNotifierProvider(
      create: (_) => authProvider,
      child: const UniShareApp(),
    ),
  );
}

class UniShareApp extends StatelessWidget {
  const UniShareApp({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    // Check if we're loading a reset password link directly from browser
    final currentUri = Uri.base;
    final isResetPasswordRoute = currentUri.path == '/reset-password' ||
                                  currentUri.pathSegments.contains('reset-password');

    return MaterialApp(
      navigatorKey: navigatorKey,
      debugShowCheckedModeBanner: false,
      title: 'UniShare',
      theme: ThemeData(primarySwatch: Colors.blue),
      // If loading reset password route, show that instead of login
      home: isResetPasswordRoute
          ? _buildResetPasswordPage()
          : (auth.isAuthenticated ? const MainPage() : const LoginPage()),
      routes: {
        '/login': (_) => const LoginPage(),
        '/register': (_) => const RegisterPage(),
        '/home': (_) => const HomePage(),
        '/forgot-password': (_) => const ForgotPasswordPage(),
        '/reset-password': (_) => _buildResetPasswordPage(),
      },
      onGenerateRoute: (settings) {
        // Handle reset password route first (before checking auth)
        if (settings.name == '/reset-password' ||
            settings.name?.contains('reset-password') == true) {
          return MaterialPageRoute(
            builder: (_) => _buildResetPasswordPage(),
            settings: settings,
          );
        }

        // Support named route for product with argument. Accept either a String id
        // or a Map containing {'itemId': '...'} for flexibility.
        if (settings.name == '/product') {
          final args = settings.arguments;
          String? itemId;
          if (args is String) {
            itemId = args;
          } else if (args is Map && args.containsKey('itemId')) {
            itemId = args['itemId']?.toString();
          }

          if (itemId != null) {
            return MaterialPageRoute(builder: (_) => ProductPage(itemId: itemId!));
          }

          // fallback: show an error page
          return MaterialPageRoute(
            builder: (_) => Scaffold(
              appBar: AppBar(title: const Text('Product')),
              body: const Center(child: Text('No product id provided')),
            ),
          );
        }

        return null; // defer to routes table
      },
    );
  }

  Widget _buildResetPasswordPage() {
    final uri = Uri.base;
    final token = uri.queryParameters['token'] ?? uri.queryParameters['code'];
    final userId = uri.queryParameters['userId'];
    return ResetPasswordPage(userId: userId, code: token);
  }
}
