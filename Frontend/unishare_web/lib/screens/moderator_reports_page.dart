import 'package:flutter/material.dart';
import 'package:unishare_web/services/api_service.dart';
import 'package:unishare_web/services/secure_storage_service.dart';
import 'package:intl/intl.dart';

class ModeratorReportsPage extends StatefulWidget {
  const ModeratorReportsPage({Key? key}) : super(key: key);

  @override
  State<ModeratorReportsPage> createState() => _ModeratorReportsPageState();
}

class _ModeratorReportsPageState extends State<ModeratorReportsPage> {
  List<Map<String, dynamic>> _reports = [];
  bool _isLoading = true;
  String? _errorMessage;
  String _filterStatus = 'ALL'; // ALL, PENDING, ACCEPTED, DECLINED

  @override
  void initState() {
    super.initState();
    _loadReports();
  }

  Future<void> _loadReports() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final token = await SecureStorageService.getAccessToken();
      if (token == null) {
        setState(() {
          _errorMessage = 'Not authenticated';
          _isLoading = false;
        });
        return;
      }

      final userId = ApiService.getUserIdFromToken(token);

      // Load reports for this moderator
      final reports = await ApiService.getReportsByModerator(userId);

      setState(() {
        _reports = reports;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _errorMessage = 'Failed to load reports: $e';
        _isLoading = false;
      });
    }
  }

  List<Map<String, dynamic>> get _filteredReports {
    if (_filterStatus == 'ALL') return _reports;
    return _reports.where((r) => r['status'] == _filterStatus).toList();
  }

  Future<void> _updateReportStatus(String reportId, String newStatus) async {
    try {
      final token = await SecureStorageService.getAccessToken();
      if (token == null) return;

      final moderatorId = ApiService.getUserIdFromToken(token);

      final result = await ApiService.updateReportStatus(
        reportId: reportId,
        status: newStatus,
        moderatorId: moderatorId,
      );

      if (!mounted) return;

      if (result['success'] == true) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Report ${newStatus.toLowerCase()} successfully'),
            backgroundColor: Colors.green,
          ),
        );
        _loadReports(); // Reload reports
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(result['message'] ?? 'Failed to update report'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Error: $e'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'PENDING':
        return Colors.orange;
      case 'ACCEPTED':
        return Colors.green;
      case 'DECLINED':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }

  IconData _getStatusIcon(String status) {
    switch (status) {
      case 'PENDING':
        return Icons.pending;
      case 'ACCEPTED':
        return Icons.check_circle;
      case 'DECLINED':
        return Icons.cancel;
      default:
        return Icons.help;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Moderator Reports'),
        backgroundColor: const Color(0xFF6366F1),
        foregroundColor: Colors.white,
      ),
      body: Column(
        children: [
          // Filter Tabs
          Container(
            color: Colors.grey[100],
            padding: const EdgeInsets.symmetric(vertical: 8),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                _buildFilterChip('ALL', _reports.length),
                const SizedBox(width: 8),
                _buildFilterChip(
                  'PENDING',
                  _reports.where((r) => r['status'] == 'PENDING').length,
                ),
                const SizedBox(width: 8),
                _buildFilterChip(
                  'ACCEPTED',
                  _reports.where((r) => r['status'] == 'ACCEPTED').length,
                ),
                const SizedBox(width: 8),
                _buildFilterChip(
                  'DECLINED',
                  _reports.where((r) => r['status'] == 'DECLINED').length,
                ),
              ],
            ),
          ),
          // Content
          Expanded(
            child: _buildContent(),
          ),
        ],
      ),
    );
  }

  Widget _buildFilterChip(String status, int count) {
    final isSelected = _filterStatus == status;
    return FilterChip(
      label: Text('$status ($count)'),
      selected: isSelected,
      onSelected: (selected) {
        setState(() {
          _filterStatus = status;
        });
      },
      selectedColor: const Color(0xFF6366F1).withOpacity(0.2),
      checkmarkColor: const Color(0xFF6366F1),
    );
  }

  Widget _buildContent() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.error_outline, size: 64, color: Colors.red[300]),
            const SizedBox(height: 16),
            Text(
              _errorMessage!,
              style: const TextStyle(fontSize: 16, color: Colors.black87),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: _loadReports,
              child: const Text('Retry'),
            ),
          ],
        ),
      );
    }

    final filteredReports = _filteredReports;

    if (filteredReports.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.inbox, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              _filterStatus == 'ALL'
                  ? 'No reports assigned to you yet'
                  : 'No $_filterStatus reports',
              style: const TextStyle(fontSize: 16, color: Colors.black54),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadReports,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: filteredReports.length,
        itemBuilder: (context, index) {
          final report = filteredReports[index];
          return _buildReportCard(report);
        },
      ),
    );
  }

  Widget _buildReportCard(Map<String, dynamic> report) {
    final reportId = report['id'] as String;
    final itemId = report['itemId'] as String;
    final userId = report['userId'] as String;
    final description = report['description'] as String;
    final status = report['status'] as String;
    final createdDate = DateTime.parse(report['createdDate']);
    final isPending = status == 'PENDING';

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header
            Row(
              children: [
                Icon(
                  _getStatusIcon(status),
                  color: _getStatusColor(status),
                  size: 24,
                ),
                const SizedBox(width: 8),
                Text(
                  status,
                  style: TextStyle(
                    color: _getStatusColor(status),
                    fontWeight: FontWeight.bold,
                    fontSize: 16,
                  ),
                ),
                const Spacer(),
                Text(
                  DateFormat('MMM dd, yyyy HH:mm').format(createdDate),
                  style: const TextStyle(
                    color: Colors.black54,
                    fontSize: 12,
                  ),
                ),
              ],
            ),
            const Divider(height: 24),
            // Report Details
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(Icons.description, size: 20, color: Colors.black54),
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Description:',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 14,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        description,
                        style: const TextStyle(fontSize: 14),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            // Item & User Info
            Row(
              children: [
                Expanded(
                  child: _buildInfoChip(
                    Icons.inventory_2,
                    'Item ID',
                    itemId.substring(0, 8),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: _buildInfoChip(
                    Icons.person,
                    'Reporter',
                    userId.substring(0, 8),
                  ),
                ),
              ],
            ),
            // Action Buttons (only for pending reports)
            if (isPending) ...[
              const SizedBox(height: 16),
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  OutlinedButton.icon(
                    onPressed: () => _showConfirmationDialog(
                      reportId,
                      'DECLINED',
                      'Decline Report',
                      'Are you sure you want to decline this report?',
                    ),
                    icon: const Icon(Icons.cancel),
                    label: const Text('Decline'),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: Colors.red,
                    ),
                  ),
                  const SizedBox(width: 8),
                  ElevatedButton.icon(
                    onPressed: () => _showConfirmationDialog(
                      reportId,
                      'ACCEPTED',
                      'Accept Report',
                      'Are you sure you want to accept this report? This may affect the item\'s visibility.',
                    ),
                    icon: const Icon(Icons.check_circle),
                    label: const Text('Accept'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.green,
                      foregroundColor: Colors.white,
                    ),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildInfoChip(IconData icon, String label, String value) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.grey[100],
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: Colors.black54),
          const SizedBox(width: 4),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    fontSize: 10,
                    color: Colors.black54,
                  ),
                ),
                Text(
                  value,
                  style: const TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w500,
                  ),
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  void _showConfirmationDialog(
    String reportId,
    String newStatus,
    String title,
    String message,
  ) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.of(context).pop();
              _updateReportStatus(reportId, newStatus);
            },
            style: ElevatedButton.styleFrom(
              backgroundColor:
                  newStatus == 'ACCEPTED' ? Colors.green : Colors.red,
              foregroundColor: Colors.white,
            ),
            child: const Text('Confirm'),
          ),
        ],
      ),
    );
  }
}

