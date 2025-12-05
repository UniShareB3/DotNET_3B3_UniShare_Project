import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:unishare_web/screens/main_page.dart';
import 'providers/auth_provider.dart';
import 'screens/login_page.dart';
import 'screens/register_page.dart';
import 'screens/home_page.dart';
import 'screens/product_page.dart';
final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();

Future<void> main() async {
  final authProvider = AuthProvider();
  await authProvider.tryAutoLogin(); // încearcă să încarce token-ul

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
    return MaterialApp(
      navigatorKey: navigatorKey,
      debugShowCheckedModeBanner: false,
      title: 'UniShare',
      theme: ThemeData(primarySwatch: Colors.blue),
      home: auth.isAuthenticated ? const MainPage() : const LoginPage(),
      routes: {
        '/login': (_) => const LoginPage(),
        '/register': (_) => const RegisterPage(),
        '/home': (_) => const HomePage(),
      },
      onGenerateRoute: (settings) {
        // Support named route for product with argument. Accept either a String id
        // or a Map containing {'itemId': '...'} for flexibility.
        if (settings.name == '/product') {
          final args = settings.arguments;
          String? itemId;
          if (args is String) itemId = args;
          else if (args is Map && args.containsKey('itemId')) itemId = args['itemId']?.toString();

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
}
