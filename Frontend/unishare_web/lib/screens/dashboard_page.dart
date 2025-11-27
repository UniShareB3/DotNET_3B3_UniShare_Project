import 'package:flutter/material.dart';
import '../services/api_service.dart';
import '../services/secure_storage_service.dart';
import 'add_item_page.dart';

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  List<Map<String, dynamic>> myItems = [];
  List<Map<String, dynamic>> requestsSent = [];
  List<Map<String, dynamic>> requestsReceived = [];

  bool _isLoading = true;
  String? _errorMessage;

  Map<String, Map<String, dynamic>> _itemCache = {};
  Map<String, Map<String, dynamic>> _userCache = {};

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    _loadData();
  }

  Future<Map<String, dynamic>> _getItem(String itemId) async {
    if (_itemCache.containsKey(itemId)) return _itemCache[itemId]!;
    final item = await ApiService.getItemById(itemId);
    _itemCache[itemId] = item;
    return item;
  }

  Future<Map<String, dynamic>> _getUser(String userId) async {
    if (_userCache.containsKey(userId)) return _userCache[userId]!;
    final user = await ApiService.getUserById(userId);
    _userCache[userId] = user;
    return user;
  }

  Future<void> _loadData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
      _itemCache = {};
      _userCache = {};
    });

    try {
      final items = await ApiService.getMyItems();
      final sent = await ApiService.getMyBookings();
      final received = await ApiService.getReceivedBookings();

      // Populam cache-ul pentru items
      for (var item in items) {
        _itemCache[item['id']] = item;
      }

      setState(() {
        myItems = items;
        requestsSent = sent;
        requestsReceived = received;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _errorMessage = "Failed to load dashboard data: $e";
        _isLoading = false;
      });
    }
  }

  Widget _buildItemCard(Map<String, dynamic> item) {
    final bool isAvailable = item['isAvailable'] ?? true;
    final String status = isAvailable ? 'Available' : 'On Loan';
    final Color statusColor = isAvailable ? Colors.green.shade600 : Colors.red.shade600;
    final String description = item['description'] ?? "No description provided.";
    final String category = item['category'] ?? "N/A";
    final String condition = item['condition'] ?? "N/A";
    final String? imageUrl = item['imageUrl'];

    Widget leadingImage;
    if (imageUrl != null && imageUrl.isNotEmpty) {
      leadingImage = CircleAvatar(
        radius: 25,
        backgroundImage: NetworkImage(imageUrl),
        backgroundColor: Colors.deepPurple.shade50,
      );
    } else {
      leadingImage = CircleAvatar(
        radius: 25,
        backgroundColor: Colors.deepPurple.shade50,
        child: const Icon(Icons.photo_library_outlined, color: Colors.deepPurple, size: 20),
      );
    }

    return Card(
      elevation: 2,
      margin: const EdgeInsets.symmetric(vertical: 8),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                leadingImage,
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(item['name'] ?? "No Name",
                          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18),
                          overflow: TextOverflow.ellipsis),
                      const SizedBox(height: 4),
                      Text(
                        description.length > 50 ? '${description.substring(0, 50)}...' : description,
                        style: TextStyle(color: Colors.grey[600], fontSize: 14),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            Wrap(
              spacing: 8.0,
              runSpacing: 4.0,
              children: [
                Chip(
                  label: Text(category),
                  backgroundColor: Colors.blue.shade50,
                  labelStyle: TextStyle(color: Colors.blue.shade800, fontSize: 12),
                ),
                Chip(
                  label: Text(condition),
                  backgroundColor: Colors.grey.shade200,
                  labelStyle: TextStyle(color: Colors.black54, fontSize: 12),
                ),
                Chip(
                  label: Text(status),
                  backgroundColor: statusColor.withOpacity(0.1),
                  labelStyle: TextStyle(color: statusColor, fontWeight: FontWeight.bold, fontSize: 12),
                  avatar: Icon(isAvailable ? Icons.check_circle_outline : Icons.schedule, color: statusColor, size: 16),
                ),
              ],
            ),
            const Divider(height: 15),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(content: Text('Managing ${item['name']}')),
                    );
                  },
                  child: const Text('Manage Item', style: TextStyle(color: Colors.deepPurple, fontWeight: FontWeight.bold)),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildBookingCard(Map<String, dynamic> booking, {required bool received}) {
    final status = booking['status'] ?? "Pending";
    Color statusColor;
    switch (status) {
      case "Approved":
        statusColor = Colors.green;
        break;
      case "Rejected":
        statusColor = Colors.red;
        break;
      default:
        statusColor = Colors.orange;
    }

    final String itemId = booking['itemId'] ?? 'N/A';
    final String otherUserId = received
        ? booking['borrowerId'] ?? 'N/A'
        : booking['ownerId'] ?? 'N/A';

    final String startDate = booking['startDate']?.toString().substring(0, 10) ?? "N/A";
    final String endDate = booking['endDate']?.toString().substring(0, 10) ?? "N/A";

    return FutureBuilder(
      future: Future.wait([
        _getItem(itemId),
        _getUser(otherUserId),
      ]),
      builder: (context, AsyncSnapshot<List<Map<String, dynamic>>> snapshot) {
        if (!snapshot.hasData || snapshot.data![0].isEmpty) {
          if (!snapshot.hasData) {
            return const Center(
                child: Padding(
                  padding: EdgeInsets.all(8.0),
                  child: LinearProgressIndicator(),
                ));
          }
          return Card(
              child: ListTile(
                title: Text(received ? 'Item Not Found' : 'Booking for Unknown Item'),
              ));
        }

        final itemDetails = snapshot.data![0];
        final userDetails = snapshot.data![1];

        final String itemTitle = itemDetails['name'] ?? "Item Not Found";
        final String? itemImageUrl = itemDetails['imageUrl'];
        final String otherUserName = received
            ? '${userDetails['firstName'] ?? 'User'} ${userDetails['lastName'] ?? ''}'.trim()
            : '${userDetails['firstName'] ?? 'Owner'} ${userDetails['lastName'] ?? ''}'.trim();

        Widget leadingImage;
        if (itemImageUrl != null && itemImageUrl.isNotEmpty) {
          leadingImage = CircleAvatar(
            radius: 25,
            backgroundImage: NetworkImage(itemImageUrl),
            backgroundColor: Colors.deepPurple.shade50,
          );
        } else {
          leadingImage = CircleAvatar(
            radius: 25,
            backgroundColor: Colors.deepPurple.shade50,
            child: const Icon(Icons.inventory_2, color: Colors.deepPurple, size: 20),
          );
        }

        return Card(
          elevation: 1,
          margin: const EdgeInsets.symmetric(vertical: 6),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          child: Padding(
            padding: const EdgeInsets.all(12.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    leadingImage,
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            itemTitle,
                            style: const TextStyle(
                                fontWeight: FontWeight.bold,
                                fontSize: 16,
                                color: Colors.deepPurple),
                            overflow: TextOverflow.ellipsis,
                          ),
                          const SizedBox(height: 4),
                          Text(
                            received
                                ? 'Request From: $otherUserName'
                                : 'Requested To: $otherUserName',
                            style: TextStyle(
                                color: Colors.black87,
                                fontSize: 13,
                                fontWeight: FontWeight.w500),
                            overflow: TextOverflow.ellipsis,
                          ),
                          const SizedBox(height: 8),
                          Row(
                            children: [
                              const Icon(Icons.calendar_month,
                                  size: 16, color: Colors.grey),
                              const SizedBox(width: 5),
                              Text("Period: $startDate to $endDate",
                                  style: TextStyle(
                                      fontSize: 13, color: Colors.grey[600])),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const Divider(height: 20),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Chip(
                      label: Text(status),
                      backgroundColor: statusColor.withOpacity(0.1),
                      labelStyle: TextStyle(
                          color: statusColor,
                          fontWeight: FontWeight.bold,
                          fontSize: 12),
                    ),
                    if (received && status == 'Pending')
                      Row(
                        children: [
                          ElevatedButton(
                            onPressed: () async {
                              final success = await ApiService.approveBooking(
                                  booking['id']);
                              if (success && mounted) {
                                ScaffoldMessenger.of(context).showSnackBar(
                                    const SnackBar(
                                        content: Text('Request Approved')));
                                _loadData();
                              }
                            },
                            style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.green,
                                foregroundColor: Colors.white,
                                padding: const EdgeInsets.symmetric(
                                    horizontal: 10)),
                            child: const Text('Approve', style: TextStyle(fontSize: 12)),
                          ),
                          const SizedBox(width: 8),
                          OutlinedButton(
                            onPressed: () async {
                              final success =
                              await ApiService.rejectBooking(booking['id']);
                              if (success && mounted) {
                                ScaffoldMessenger.of(context).showSnackBar(
                                    const SnackBar(
                                        content: Text('Request Rejected')));
                                _loadData();
                              }
                            },
                            style: OutlinedButton.styleFrom(
                              foregroundColor: Colors.red,
                              side: const BorderSide(color: Colors.red),
                              padding: const EdgeInsets.symmetric(horizontal: 10),
                            ),
                            child:
                            const Text('Reject', style: TextStyle(fontSize: 12)),
                          ),
                        ],
                      )
                    else if (!received && status == 'Pending')
                      TextButton(
                        onPressed: () async {
                          final success =
                          await ApiService.updateBookingStatus(
                              bookingId: booking['id'], newStatus: 'Cancelled');
                          if (success && mounted) _loadData();
                        },
                        child: const Text('Cancel Request',
                            style: TextStyle(color: Colors.red)),
                      ),
                  ],
                ),
              ],
            ),
          ),
        );
      },
    );
  }


  Widget _buildTabContent(List<Map<String, dynamic>> list, {required bool isBooking, bool received = false}) {
    if (_isLoading) return const Center(child: CircularProgressIndicator());
    if (_errorMessage != null) return Center(child: Text(_errorMessage!));

    if (list.isEmpty) {
      String message = isBooking ? (received ? "No requests received yet." : "No requests sent yet.") : "You haven't listed any items yet. Tap '+' to add one!";
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(isBooking ? Icons.notification_important_outlined : Icons.inventory_2_outlined, size: 60, color: Colors.grey[400]),
            const SizedBox(height: 10),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 40.0),
              child: Text(message, textAlign: TextAlign.center, style: TextStyle(fontSize: 16, color: Colors.grey[600])),
            ),
          ],
        ),
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(10),
      itemCount: list.length,
      itemBuilder: (_, i) => isBooking ? _buildBookingCard(list[i], received: received) : _buildItemCard(list[i]),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("Dashboard", style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: Colors.deepPurple,
        foregroundColor: Colors.white,
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: Colors.white,
          labelColor: Colors.white,
          unselectedLabelColor: Colors.white70,
          tabs: const [
            Tab(text: "My Items", icon: Icon(Icons.list_alt)),
            Tab(text: "Sent", icon: Icon(Icons.send)),
            Tab(text: "Received", icon: Icon(Icons.call_received)),
          ],
        ),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _loadData),
        ],
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          _buildTabContent(myItems, isBooking: false),
          _buildTabContent(requestsSent, isBooking: true, received: false),
          _buildTabContent(requestsReceived, isBooking: true, received: true),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final created = await Navigator.push(context, MaterialPageRoute(builder: (_) => const AddItemPage()));
          if (created == true && mounted) _loadData();
        },
        backgroundColor: Colors.deepPurple,
        foregroundColor: Colors.white,
        child: const Icon(Icons.add),
      ),
    );
  }
}
