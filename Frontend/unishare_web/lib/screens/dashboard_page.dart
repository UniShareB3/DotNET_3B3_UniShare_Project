import 'package:flutter/material.dart';
import '../services/api_service.dart';
import '../services/secure_storage_service.dart';
import 'add_item_page.dart';
import 'product_page.dart';

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
    // 5 tab-uri: My Items, Sent, Received, Lent, Borrowed
    _tabController = TabController(length: 5, vsync: this);
    _loadData();
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
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
      final items = await ApiService.getMyItems().timeout(const Duration(seconds: 10));
      final sent = await ApiService.getMyBookings().timeout(const Duration(seconds: 10));
      final received = await ApiService.getReceivedBookings().timeout(const Duration(seconds: 10));

      for (var item in items) {
        _itemCache[item['id']] = item;
      }

      setState(() {
        myItems = items;
        requestsSent = sent;
        requestsReceived = received;
      });
    } catch (e) {
      print('Dashboard _loadData error: $e');
      setState(() {
        _errorMessage = "Failed to load dashboard data.";
      });
    } finally {
      if (mounted) setState(() { _isLoading = false; });
    }
  }

  bool _bookingIsApproved(Map<String, dynamic> b) {
    final bs = b['bookingStatus'];
    if (bs is int) return bs == 1;
    if (bs is String) {
      final s = bs.toLowerCase();
      if (s == '1' || s == 'approved') return true;
    }
    final s2 = b['status']?.toString().toLowerCase();
    if (s2 != null) return s2 == 'approved';
    return false;
  }

  List<Map<String, dynamic>> _lentBookings() {
    return requestsReceived.where((b) => _bookingIsApproved(b)).toList();
  }

  List<Map<String, dynamic>> _borrowedBookings() {
    return requestsSent.where((b) => _bookingIsApproved(b)).toList();
  }

  Widget _buildItemCard(Map<String, dynamic> item) {
    final String description = item['description'] ?? "No description provided.";
    final String category = item['category'] ?? "N/A";
    final String condition = item['condition'] ?? "N/A";
    final String? imageUrl = item['imageUrl'];

    // Design: Imagine dreptunghiulară cu colțuri rotunjite (ClipRRect)
    Widget leadingImage;
    if (imageUrl != null && imageUrl.isNotEmpty) {
      leadingImage = ClipRRect(
        borderRadius: BorderRadius.circular(8),
        child: Image.network(
          imageUrl,
          width: 70,
          height: 70,
          fit: BoxFit.cover,
          errorBuilder: (ctx, err, stack) => Container(
            width: 70,
            height: 70,
            color: Colors.grey[200],
            child: const Icon(Icons.broken_image, color: Colors.grey),
          ),
        ),
      );
    } else {
      leadingImage = Container(
        width: 70,
        height: 70,
        decoration: BoxDecoration(
          color: Colors.deepPurple.shade50,
          borderRadius: BorderRadius.circular(8),
        ),
        child: const Icon(Icons.inventory_2_outlined, color: Colors.deepPurple, size: 30),
      );
    }

    return Card(
      elevation: 2,
      margin: const EdgeInsets.symmetric(vertical: 8, horizontal: 4),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                leadingImage,
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        item['name'] ?? "No Name",
                        style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      const SizedBox(height: 4),
                      Text(
                        description,
                        style: TextStyle(color: Colors.grey[600], fontSize: 13),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            // Tags / Chips simplificate
            Wrap(
              spacing: 8.0,
              runSpacing: 4.0,
              children: [
                _buildTag(category, Colors.blue),
                _buildTag(condition, Colors.orange),
              ],
            ),
            const SizedBox(height: 8),
            const Divider(),
            Align(
              alignment: Alignment.centerRight,
              child: TextButton.icon(
                onPressed: () {
                  final itemId = item['id']?.toString();
                  if (itemId != null && itemId.isNotEmpty) {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (_) => ProductPage(itemId: itemId),
                      ),
                    );
                  }
                },
                icon: const Icon(Icons.edit, size: 16),
                label: const Text('Manage Item', style: TextStyle(fontWeight: FontWeight.w600)),
                style: TextButton.styleFrom(foregroundColor: Colors.deepPurple),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTag(String text, MaterialColor color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.shade50,
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: color.shade100),
      ),
      child: Text(
        text,
        style: TextStyle(color: color.shade800, fontSize: 11, fontWeight: FontWeight.w600),
      ),
    );
  }

  Widget _buildBookingCard(Map<String, dynamic> booking, {required bool received, bool showItemMeta = false, bool allowFinish = false}) {
    try {
      if (!booking.containsKey('bookingStatus') || booking['bookingStatus'] == null) {
        booking['bookingStatus'] = 0;
      }
    } catch (e) {
      // ignore
    }

    String _bookingStatusLabel(Map<String, dynamic> b) {
      final s = b['status'];
      if (s != null) return s.toString();
      final bs = b['bookingStatus'];
      if (bs is int) {
        return switch (bs) {
          1 => 'Approved',
          2 => 'Rejected',
          3 => 'Completed',
          4 => 'Canceled',
          _ => 'Pending'
        };
      }
      return 'Pending';
    }

    final status = _bookingStatusLabel(booking);
    Color statusColor;
    switch (status) {
      case 'Approved': statusColor = Colors.green; break;
      case 'Rejected': statusColor = Colors.red; break;
      case 'Completed': statusColor = Colors.grey; break;
      case 'Canceled': statusColor = Colors.red; break;
      default: statusColor = Colors.orange;
    }

    final String itemId = booking['itemId']?.toString() ?? '';
    String? otherUserId;
    if (received) {
      otherUserId = booking['borrowerId']?.toString();
    } else {
      otherUserId = booking['ownerId']?.toString() ?? (booking['item'] is Map ? (booking['item']['ownerId']?.toString()) : null);
    }

    final String startDate = booking['startDate']?.toString().substring(0, 10) ?? "N/A";
    final String endDate = booking['endDate']?.toString().substring(0, 10) ?? "N/A";

    final futures = <Future<Map<String, dynamic>>>[];
    futures.add(_getItem(itemId));
    final bool willFetchUser = otherUserId != null && otherUserId.isNotEmpty && otherUserId != 'N/A';
    if (willFetchUser) futures.add(_getUser(otherUserId!));

    return FutureBuilder(
      future: Future.wait(futures),
      builder: (context, AsyncSnapshot<List<Map<String, dynamic>>> snapshot) {
        if (!snapshot.hasData) {
          return Card(
            margin: const EdgeInsets.symmetric(vertical: 6),
            child: Container(height: 100, alignment: Alignment.center, child: const CircularProgressIndicator()),
          );
        }
        if (snapshot.data!.isEmpty || snapshot.data![0].isEmpty) {
          return const Card(child: ListTile(title: Text('Item info unavailable')));
        }

        final itemDetails = snapshot.data![0];
        final Map<String, dynamic> userDetails = willFetchUser && snapshot.data!.length > 1 ? snapshot.data![1] : {};

        final String itemTitle = itemDetails['name'] ?? "Item Not Found";
        final String? itemImageUrl = itemDetails['imageUrl'];

        String otherUserName = 'Unknown';
        if (userDetails.isNotEmpty) {
          otherUserName = received
              ? '${userDetails['firstName'] ?? 'User'} ${userDetails['lastName'] ?? ''}'
              : '${userDetails['firstName'] ?? 'Owner'} ${userDetails['lastName'] ?? ''}';
        } else if (received) {
          otherUserName = booking['borrowerName'] ?? 'User';
        } else {
          otherUserName = booking['ownerName'] ?? 'Owner';
        }

        Widget leadingImage;
        if (itemImageUrl != null && itemImageUrl.isNotEmpty) {
          leadingImage = ClipRRect(
            borderRadius: BorderRadius.circular(8),
            child: Image.network(
              itemImageUrl,
              width: 60,
              height: 60,
              fit: BoxFit.cover,
            ),
          );
        } else {
          leadingImage = Container(
            width: 60,
            height: 60,
            decoration: BoxDecoration(
              color: Colors.deepPurple.shade50,
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Icon(Icons.inventory_2, color: Colors.deepPurple),
          );
        }

        return Card(
          elevation: 2,
          margin: const EdgeInsets.symmetric(vertical: 6, horizontal: 4),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          child: Padding(
            padding: const EdgeInsets.all(12.0),
            child: Column(
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
                            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                            overflow: TextOverflow.ellipsis,
                          ),
                          const SizedBox(height: 4),
                          Text(
                            received ? 'From: $otherUserName' : 'To: $otherUserName',
                            style: TextStyle(color: Colors.grey[700], fontSize: 13),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            "$startDate  ➔  $endDate",
                            style: TextStyle(fontSize: 12, color: Colors.grey[600], fontStyle: FontStyle.italic),
                          ),
                        ],
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                      decoration: BoxDecoration(
                        color: statusColor.withOpacity(0.1),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(
                        status,
                        style: TextStyle(color: statusColor, fontWeight: FontWeight.bold, fontSize: 11),
                      ),
                    ),
                  ],
                ),

                if ((received && status == 'Pending') || (!received && status == 'Pending') || (allowFinish && status == 'Approved')) ...[
                  const Divider(height: 20),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      if (received && status == 'Pending') ...[
                        OutlinedButton.icon(
                          onPressed: () => _updateBookingStatus(booking['id'], 2, 'Rejected'),
                          icon: const Icon(Icons.close, size: 16),
                          label: const Text('Reject'),
                          style: OutlinedButton.styleFrom(
                            foregroundColor: Colors.red,
                            side: const BorderSide(color: Colors.red),
                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 0),
                          ),
                        ),
                        const SizedBox(width: 8),
                        ElevatedButton.icon(
                          onPressed: () => _updateBookingStatus(booking['id'], 1, 'Approved'),
                          icon: const Icon(Icons.check, size: 16),
                          label: const Text('Approve'),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: Colors.green,
                            foregroundColor: Colors.white,
                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 0),
                          ),
                        ),
                      ]
                      else if (!received && status == 'Pending') ...[
                        TextButton.icon(
                          onPressed: () => _updateBookingStatus(booking['id'], 4, 'Cancelled'),
                          icon: const Icon(Icons.cancel_outlined, size: 16),
                          label: const Text('Cancel Request'),
                          style: TextButton.styleFrom(foregroundColor: Colors.red),
                        ),
                      ]
                      else if (allowFinish && status == 'Approved') ...[
                          ElevatedButton.icon(
                            onPressed: () async {
                              final confirm = await showDialog<bool>(
                                context: context,
                                builder: (ctx) => AlertDialog(
                                  title: const Text('Return Item?'),
                                  content: const Text('Confirm that you have returned this item.'),
                                  actions: [
                                    TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('No')),
                                    TextButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Yes, Returned')),
                                  ],
                                ),
                              );
                              if (confirm == true) {
                                _updateBookingStatus(booking['id'], 3, 'Finished');
                              }
                            },
                            icon: const Icon(Icons.done_all, size: 16),
                            label: const Text('Finish & Return'),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: Colors.deepPurple,
                              foregroundColor: Colors.white,
                            ),
                          ),
                        ],
                    ],
                  ),
                ],
              ],
            ),
          ),
        );
      },
    );
  }

  Future<void> _updateBookingStatus(dynamic id, int status, String actionLabel) async {
    final result = await ApiService.updateBookingResult(id.toString(), status);
    if (mounted) {
      if (result['success'] == true) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Request $actionLabel successfully'), backgroundColor: Colors.green));
        _loadData();
      } else {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(result['message'] ?? 'Error'), backgroundColor: Colors.red));
      }
    }
  }

  Widget _buildTabContent(List<Map<String, dynamic>> list, {required bool isBooking, bool received = false, bool showItemMeta = false, bool allowFinish = false}) {
    if (_isLoading) return const Center(child: CircularProgressIndicator());
    if (_errorMessage != null) return Center(child: Text(_errorMessage!, style: const TextStyle(color: Colors.red)));

    if (list.isEmpty) {
      String message = isBooking ? (received ? "No requests received yet." : "No requests sent yet.") : "You haven't listed any items yet.";
      IconData icon = isBooking ? (received ? Icons.inbox : Icons.outbox) : Icons.add_business;

      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(color: Colors.grey.shade100, shape: BoxShape.circle),
              child: Icon(icon, size: 50, color: Colors.grey.shade400),
            ),
            const SizedBox(height: 16),
            Text(message, style: TextStyle(fontSize: 16, color: Colors.grey[600])),
          ],
        ),
      );
    }

    if (isBooking && received && list.isNotEmpty && list[0].containsKey('name') && list[0].containsKey('ownerName')) {
      return ListView.builder(
        padding: const EdgeInsets.all(10),
        itemCount: list.length,
        itemBuilder: (_, i) => _buildItemCard(list[i]),
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(12),
      itemCount: list.length,
      itemBuilder: (_, i) => isBooking
          ? _buildBookingCard(list[i], received: received, showItemMeta: showItemMeta, allowFinish: allowFinish)
          : _buildItemCard(list[i]),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("Dashboard", style: TextStyle(fontWeight: FontWeight.bold)),
        centerTitle: false,
        backgroundColor: Colors.deepPurple,
        foregroundColor: Colors.white,
        elevation: 0,
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: Colors.amber,
          indicatorWeight: 3,
          labelColor: Colors.white,
          unselectedLabelColor: Colors.white60,
          labelStyle: const TextStyle(fontWeight: FontWeight.bold),
          tabs: const [
            Tab(text: "My Items"),
            Tab(text: "Sent"),
            Tab(text: "Received"),
            Tab(text: "Lent"),
            Tab(text: "Borrowed"),
          ],
        ),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _loadData, tooltip: 'Refresh Data'),
        ],
      ),
      body: Container(
        color: Colors.grey.shade50,
        child: TabBarView(
          controller: _tabController,
          children: [
            _buildTabContent(myItems, isBooking: false),
            _buildTabContent(requestsSent, isBooking: true, received: false),
            _buildTabContent(requestsReceived, isBooking: true, received: true),
            _buildTabContent(_lentBookings(), isBooking: true, received: true, showItemMeta: true),
            _buildTabContent(_borrowedBookings(), isBooking: true, received: false, showItemMeta: true, allowFinish: true),
          ],
        ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          final created = await Navigator.push(context, MaterialPageRoute(builder: (_) => const AddItemPage()));
          if (created == true && mounted) _loadData();
        },
        backgroundColor: Colors.deepPurple,
        foregroundColor: Colors.white,
        icon: const Icon(Icons.add),
        label: const Text('Add Item'),
      ),
    );
  }
}