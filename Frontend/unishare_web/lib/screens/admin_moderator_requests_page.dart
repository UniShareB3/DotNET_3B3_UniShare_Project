import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../services/api_service.dart';
import '../services/secure_storage_service.dart';

class AdminModeratorRequestsPage extends StatefulWidget {
  const AdminModeratorRequestsPage({Key? key}) : super(key: key);

  @override
  State<AdminModeratorRequestsPage> createState() => _AdminModeratorRequestsPageState();
}

class _AdminModeratorRequestsPageState extends State<AdminModeratorRequestsPage> {
  List<Map<String, dynamic>> _requests = [];
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadRequests();
  }

  Future<void> _loadRequests() async {
    setState(() { _isLoading = true; _error = null; });
    try {
      final reqs = await ApiService.getAllModeratorRequests();
      setState(() { _requests = reqs; _isLoading = false; });
    } catch (e) {
      setState(() { _error = e.toString(); _isLoading = false; });
    }
  }

  Color _statusColor(String status) {
    switch (status.toUpperCase()) {
      case 'PENDING': return Colors.orange;
      case 'ACCEPTED': return Colors.green;
      case 'REJECTED': return Colors.red;
      default: return Colors.grey;
    }
  }

  IconData _statusIcon(String status) {
    switch (status.toUpperCase()) {
      case 'PENDING': return Icons.pending;
      case 'ACCEPTED': return Icons.check_circle;
      case 'REJECTED': return Icons.cancel;
      default: return Icons.help;
    }
  }

  Future<void> _handleAction(String requestId, bool accept) async {
    setState(() { _isLoading = true; });
    try {
      final token = await SecureStorageService.getAccessToken();
      final adminId = ApiService.getUserIdFromToken(token);
      final statusValue = accept ? 1 : 2; // 1 ACCEPTED, 2 REJECTED
      final res = await ApiService.updateModeratorRequestStatus(
        requestId: requestId,
        statusValue: statusValue,
        adminId: adminId,
      );

      if (res['success'] == true) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(accept ? 'Request accepted' : 'Request rejected'), backgroundColor: Colors.green),
        );
        await _loadRequests();
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(res['message'] ?? 'Action failed'), backgroundColor: Colors.red),
        );
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red),
      );
    } finally {
      if (mounted) setState(() { _isLoading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Moderator Requests (Admin)'),
        backgroundColor: Colors.deepPurple,
        foregroundColor: Colors.white,
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(child: Text('Error: $_error'))
              : _requests.isEmpty
                  ? const Center(child: Text('No moderator requests'))
                  : RefreshIndicator(
                      onRefresh: _loadRequests,
                      child: ListView.builder(
                        padding: const EdgeInsets.all(16),
                        itemCount: _requests.length,
                        itemBuilder: (context, index) {
                          final r = _requests[index];
                          final id = r['id']?.toString() ?? '';
                          final userId = r['userId']?.toString() ?? '';
                          final reason = r['reason'] ?? '';
                          final status = r['status']?.toString() ?? '';
                          final created = r['createdDate'] != null
                              ? DateTime.parse(r['createdDate']).toLocal()
                              : null;

                          return Card(
                            margin: const EdgeInsets.only(bottom: 12),
                            child: Padding(
                              padding: const EdgeInsets.all(12),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    children: [
                                      Icon(_statusIcon(status), color: _statusColor(status)),
                                      const SizedBox(width: 8),
                                      Text(status, style: TextStyle(color: _statusColor(status), fontWeight: FontWeight.bold)),
                                      const Spacer(),
                                      if (created != null) Text(DateFormat('yyyy-MM-dd HH:mm').format(created), style: const TextStyle(fontSize: 12, color: Colors.black54)),
                                    ],
                                  ),
                                  const SizedBox(height: 8),
                                  Text('User: ${userId.substring(0, 8)}', style: const TextStyle(fontSize: 13)),
                                  const SizedBox(height: 8),
                                  Text('Reason:', style: const TextStyle(fontWeight: FontWeight.bold)),
                                  const SizedBox(height: 4),
                                  Text(reason),
                                  const SizedBox(height: 12),
                                  if (status.toUpperCase() == 'PENDING') Row(
                                    mainAxisAlignment: MainAxisAlignment.end,
                                    children: [
                                      OutlinedButton(
                                        onPressed: () => _handleAction(id, false),
                                        style: OutlinedButton.styleFrom(foregroundColor: Colors.red),
                                        child: const Text('Reject'),
                                      ),
                                      const SizedBox(width: 8),
                                      ElevatedButton(
                                        onPressed: () => _handleAction(id, true),
                                        style: ElevatedButton.styleFrom(backgroundColor: Colors.green),
                                        child: const Text('Accept'),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          );
                        },
                      ),
                    ),
    );
  }
}

