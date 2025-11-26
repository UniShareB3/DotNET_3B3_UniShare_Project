import 'package:flutter/material.dart';
import 'add_item_page.dart'; // import AddItemPage

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

// Clasa Item reflectă structura C#
class Item {
  final String id;
  final String name;
  final String description;
  final String category;
  final String condition;
  final bool isAvailable;
  final int requests;
  final String? imageUrl;

  Item(this.id, this.name, this.description, this.category, this.condition, this.isAvailable, this.requests, this.imageUrl);
}

class Request {
  final String itemId;
  final String itemTitle;
  final String user;
  final String status;

  Request(this.itemId, this.itemTitle, this.user, this.status);
}

class _DashboardPageState extends State<DashboardPage> with SingleTickerProviderStateMixin {
  late TabController _tabController;

  List<Item> _myItems = [];
  List<Request> _receivedRequests = [];
  List<Request> _sentRequests = [];
  bool _isLoading = true;
  String? _errorMessage;

  // Mock data temporar
  final List<Item> _mockItems = [
    Item('1', 'Flutter Development Book', 'Latest edition for cross-platform development.', 'Book', 'Excellent', true, 3, 'https://placehold.co/100x100/A05AEC/ffffff?text=Book'),
    Item('2', 'Projector for Presentation', 'High-luminosity projector, perfect for lectures.', 'Electronics', 'Good', false, 0, 'https://placehold.co/100x100/A05AEC/ffffff?text=Proj'),
    Item('3', 'External Hard Drive 1TB', 'Reliable storage device.', 'Electronics', 'Fair', true, 1, null),
  ];

  final List<Request> _mockReceivedRequests = [
    Request('3', 'External Hard Drive 1TB', 'Jane Doe', 'Pending'),
    Request('1', 'Flutter Development Book', 'Alex Smith', 'Pending'),
  ];

  final List<Request> _mockSentRequests = [
    Request('10', 'Advanced Math Textbook', 'Mark Johnson', 'Approved'),
    Request('11', 'VR Headset', 'Sarah Lee', 'Pending'),
  ];

  Future<void> _fetchData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      await Future.delayed(const Duration(milliseconds: 800));
      setState(() {
        _myItems = _mockItems;
        _receivedRequests = _mockReceivedRequests;
        _sentRequests = _mockSentRequests;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _errorMessage = 'Failed to load dashboard data. Please try again.';
        _isLoading = false;
      });
    }
  }

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    _fetchData();
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  Widget _buildItemCard(BuildContext context, Item item) {
    final String mainStatus = item.isAvailable ? 'Available' : 'On Loan';
    final Color statusColor = item.isAvailable ? Colors.green.shade600 : Colors.red.shade600;

    Widget leadingImage = (item.imageUrl != null && item.imageUrl!.isNotEmpty)
        ? CircleAvatar(radius: 25, backgroundImage: NetworkImage(item.imageUrl!), backgroundColor: Colors.deepPurple.shade50)
        : CircleAvatar(radius: 25, backgroundColor: Colors.deepPurple.shade50, child: const Icon(Icons.photo_library_outlined, color: Colors.deepPurple, size: 20));

    return Card(
      elevation: 2,
      margin: const EdgeInsets.symmetric(vertical: 8),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(12.0),
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
                      Text(item.name, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18), overflow: TextOverflow.ellipsis),
                      const SizedBox(height: 4),
                      Text(
                        item.description.length > 50 ? '${item.description.substring(0, 50)}...' : item.description,
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
                Chip(label: Text(item.category), backgroundColor: Colors.blue.shade50, labelStyle: TextStyle(color: Colors.blue.shade800, fontSize: 12)),
                Chip(label: Text(item.condition), backgroundColor: Colors.grey.shade200, labelStyle: TextStyle(color: Colors.black54, fontSize: 12)),
                Chip(
                  label: Text(mainStatus),
                  backgroundColor: statusColor.withOpacity(0.1),
                  labelStyle: TextStyle(color: statusColor, fontWeight: FontWeight.bold, fontSize: 12),
                  avatar: Icon(item.isAvailable ? Icons.check_circle_outline : Icons.schedule, color: statusColor, size: 16),
                ),
              ],
            ),
            const Divider(height: 20),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                item.requests > 0
                    ? Chip(
                  label: Text('${item.requests} Pending Requests'),
                  backgroundColor: Colors.amber.shade100,
                  labelStyle: TextStyle(color: Colors.amber.shade900, fontWeight: FontWeight.bold),
                  avatar: const Icon(Icons.notifications_active, color: Colors.amber, size: 16),
                )
                    : const SizedBox.shrink(),
                TextButton(
                  onPressed: () async {
                    final edited = await Navigator.push(
                      context,
                      MaterialPageRoute(builder: (_) => const AddItemPage()),
                    );
                    if (edited == true) {
                      _fetchData();
                    }
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

  Widget _buildRequestCard(BuildContext context, Request request, {required bool isReceived}) {
    Color statusColor;
    switch (request.status) {
      case 'Approved':
        statusColor = Colors.green;
        break;
      case 'Rejected':
        statusColor = Colors.red;
        break;
      default:
        statusColor = Colors.orange;
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
            Text(isReceived ? 'Request from: ${request.user}' : 'Item: ${request.itemTitle}', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
            const SizedBox(height: 4),
            Text(isReceived ? 'Item: ${request.itemTitle}' : 'Requested from: ${request.user}', style: TextStyle(color: Colors.grey[600])),
            const SizedBox(height: 8),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Chip(label: Text(request.status), backgroundColor: statusColor.withOpacity(0.1), labelStyle: TextStyle(color: statusColor, fontWeight: FontWeight.bold)),
                if (isReceived && request.status == 'Pending')
                  Row(
                    children: [
                      ElevatedButton(
                        onPressed: () {
                          ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Approved request from ${request.user}')));
                        },
                        style: ElevatedButton.styleFrom(backgroundColor: Colors.green, foregroundColor: Colors.white, padding: const EdgeInsets.symmetric(horizontal: 10)),
                        child: const Text('Approve', style: TextStyle(fontSize: 12)),
                      ),
                      const SizedBox(width: 8),
                      OutlinedButton(
                        onPressed: () {
                          ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Rejected request from ${request.user}')));
                        },
                        style: OutlinedButton.styleFrom(foregroundColor: Colors.red, side: const BorderSide(color: Colors.red), padding: const EdgeInsets.symmetric(horizontal: 10)),
                        child: const Text('Reject', style: TextStyle(fontSize: 12)),
                      ),
                    ],
                  )
                else if (!isReceived && request.status == 'Pending')
                  TextButton(
                    onPressed: () {
                      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Cancelled request for ${request.itemTitle}')));
                    },
                    child: const Text('Cancel Request', style: TextStyle(color: Colors.red)),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildEmptyState(String message, IconData icon) {
    return Center(child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [Icon(icon, size: 60, color: Colors.grey[400]), const SizedBox(height: 10), Text(message, textAlign: TextAlign.center, style: TextStyle(fontSize: 16, color: Colors.grey[600]))]));
  }

  Widget _buildErrorState(String message, IconData icon) {
    return Center(
      child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [
        Icon(icon, size: 60, color: Colors.red[400]),
        const SizedBox(height: 10),
        Text(message, textAlign: TextAlign.center, style: TextStyle(fontSize: 16, color: Colors.red[600])),
        const SizedBox(height: 20),
        ElevatedButton(
          onPressed: _fetchData,
          style: ElevatedButton.styleFrom(backgroundColor: Colors.deepPurple, foregroundColor: Colors.white),
          child: const Text('Retry'),
        ),
      ]),
    );
  }

  Widget _buildMyItemsTab() {
    if (_isLoading) return const Center(child: CircularProgressIndicator());
    if (_errorMessage != null) return _buildErrorState(_errorMessage!, Icons.error_outline);
    if (_myItems.isEmpty) return _buildEmptyState('You haven\'t listed any items yet.', Icons.inventory_2_outlined);
    return ListView.builder(
      padding: const EdgeInsets.only(top: 10, bottom: 20),
      itemCount: _myItems.length,
      itemBuilder: (context, index) => _buildItemCard(context, _myItems[index]),
    );
  }

  Widget _buildRequestsReceivedTab() {
    if (_isLoading) return const Center(child: CircularProgressIndicator());
    if (_receivedRequests.isEmpty) return _buildEmptyState('No new borrow requests received.', Icons.inbox_outlined);
    return ListView.builder(
      padding: const EdgeInsets.only(top: 10, bottom: 20),
      itemCount: _receivedRequests.length,
      itemBuilder: (context, index) => _buildRequestCard(context, _receivedRequests[index], isReceived: true),
    );
  }

  Widget _buildRequestsSentTab() {
    if (_isLoading) return const Center(child: CircularProgressIndicator());
    if (_sentRequests.isEmpty) return _buildEmptyState('You haven\'t sent any borrow requests yet.', Icons.outbox_outlined);
    return ListView.builder(
      padding: const EdgeInsets.only(top: 10, bottom: 20),
      itemCount: _sentRequests.length,
      itemBuilder: (context, index) => _buildRequestCard(context, _sentRequests[index], isReceived: false),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 20),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text('Your Dashboard', style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold, color: Colors.deepPurple)),
                ElevatedButton.icon(
                  onPressed: () async {
                    final created = await Navigator.push(context, MaterialPageRoute(builder: (_) => const AddItemPage()));
                    if (created == true) _fetchData();
                  },
                  icon: const Icon(Icons.add, color: Colors.white),
                  label: const Text('List New Item', style: TextStyle(color: Colors.white)),
                  style: ElevatedButton.styleFrom(backgroundColor: Colors.deepPurple, shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)), elevation: 5),
                ),
              ],
            ),
            const SizedBox(height: 20),
            TabBar(
              controller: _tabController,
              indicatorColor: Colors.deepPurple,
              labelColor: Colors.deepPurple,
              unselectedLabelColor: Colors.grey,
              labelStyle: const TextStyle(fontWeight: FontWeight.bold),
              tabs: const [
                Tab(icon: Icon(Icons.list_alt), text: 'My Items'),
                Tab(icon: Icon(Icons.call_received), text: 'Requests Received'),
                Tab(icon: Icon(Icons.send_time_extension), text: 'Requests Sent'),
              ],
            ),
            Expanded(
              child: TabBarView(
                controller: _tabController,
                children: [_buildMyItemsTab(), _buildRequestsReceivedTab(), _buildRequestsSentTab()],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
