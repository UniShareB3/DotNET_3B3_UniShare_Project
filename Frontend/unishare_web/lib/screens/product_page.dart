import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:table_calendar/table_calendar.dart';
import '../services/api_service.dart';
import 'package:unishare_web/services/secure_storage_service.dart';
import 'package:unishare_web/screens/report_item_dialog.dart';
import 'chat_page.dart';
import 'edit_item_page.dart'; // <--- IMPORTANT: Asigură-te că ai acest import

class ProductPage extends StatefulWidget {
  final String itemId;
  const ProductPage({super.key, required this.itemId});

  @override
  State<ProductPage> createState() => _ProductPageState();
}

class _ProductPageState extends State<ProductPage> {
  // Maps aligned with backend enums
  static const Map<int, String> _categoryMap = {
    0: 'Others', 1: 'Books', 2: 'Electronics', 3: 'Kitchen',
    4: 'Clothing', 5: 'Accessories',
  };

  static const Map<int, String> _conditionMap = {
    0: 'New', 1: 'Excellent', 2: 'Good', 3: 'Fair', 4: 'Poor',
  };

  DateTime? _selectedStartDate;
  DateTime? _selectedEndDate;
  bool _isBookingLoading = false;

  String _mapIntOrStringToName(dynamic value, Map<int, String> map, String fallback) {
    if (value == null) return fallback;
    if (value is int) return map[value] ?? value.toString();
    if (value is String) {
      final trimmed = value.trim();
      final parsed = int.tryParse(trimmed);
      if (parsed != null) return map[parsed] ?? trimmed;
      return trimmed;
    }
    return fallback;
  }

  Map<String, dynamic>? item;
  List<Map<String, dynamic>> bookings = [];
  List<Map<String, dynamic>> _reviews = [];
  Map<String, dynamic>? _myReview;
  bool _hasUserReview = false;
  String? _currentUserId;
  final Map<String, GlobalKey> _reviewKeys = {};
  final GlobalKey _reviewsSectionKey = GlobalKey();
  Set<DateTime> blockedDays = {};
  bool loading = true;
  String? error;
  int _acceptedReportsCount = 0;

  @override
  void initState() {
    super.initState();
    _load();
    _loadReviews();
  }

  Future<void> _load() async {
    setState(() {
      loading = true;
      error = null;
    });

    try {
      // 1. Fetch Item Data
      final it = await ApiService.getItemById(widget.itemId);
      print('DEBUG: Item fetched: $it');

      // 2. Fetch User ID immediately to determine ownership
      final token = await SecureStorageService.getAccessToken();

      String? uid;
      if (token != null && token.isNotEmpty) {
        try {
          uid = ApiService.getUserIdFromToken(token)?.toString();
        } catch (e) {
          uid = null;
        }
      }

      // 3. Fetch bookings and reports
      final itemBookings = await ApiService.getBookingsForItem(widget.itemId);
      final reportsCount = await ApiService.getAcceptedReportsCount(
        itemId: widget.itemId,
        numberOfDays: 7,
      );

      setState(() {
        item = it;
        bookings = itemBookings.map((b) => Map<String, dynamic>.from(b)).toList();
        blockedDays = _computeBlockedDays(bookings);
        _acceptedReportsCount = reportsCount;
        _currentUserId = uid;
        loading = false;
      });
    } catch (e) {
      setState(() {
        error = e.toString();
        loading = false;
      });
    }
  }

  Future<void> _loadReviews() async {
    try {
      final reviews = await ApiService.getReviewsForItem(widget.itemId);

      Map<String, dynamic>? myReview;
      bool hasReview = false;

      final normalizedUserId = _currentUserId?.trim().toLowerCase();

      if (normalizedUserId != null) {
        for (var r in reviews) {
          final reviewerId = r['reviewerId']?.toString().trim().toLowerCase();
          if (reviewerId != null && reviewerId == normalizedUserId) {
            hasReview = true;
            myReview = r;
            break;
          }
        }
      }

      for (var r in reviews) {
        final id = r['id']?.toString();
        if (id != null && !_reviewKeys.containsKey(id)) {
          _reviewKeys[id] = GlobalKey();
        }
      }

      setState(() {
        _reviews = reviews;
        _hasUserReview = hasReview;
        _myReview = myReview;
      });
    } catch (e) {
      print('Failed to load reviews: $e');
    }
  }

  Set<DateTime> _computeBlockedDays(List<Map<String, dynamic>> bookings) {
    final Set<DateTime> out = {};

    int? _statusFromDynamic(dynamic s) {
      if (s == null) return null;
      if (s is int) return s;
      if (s is String) {
        final t = s.trim();
        final p = int.tryParse(t);
        if (p != null) return p;
        switch (t.toLowerCase()) {
          case 'pending': return 0;
          case 'approved': return 1;
          case 'rejected': return 2;
          case 'completed': return 3;
          case 'canceled':
          case 'cancelled': return 4;
          default: return null;
        }
      }
      return null;
    }

    for (var b in bookings) {
      try {
        final dynStatus = b['bookingStatus'] ?? b['BookingStatus'] ?? b['status'] ?? b['Status'];
        final statusInt = _statusFromDynamic(dynStatus);

        if (statusInt == 2 || statusInt == 4) continue;

        final s = DateTime.parse(b['startDate'].toString()).toUtc();
        final e = DateTime.parse(b['endDate'].toString()).toUtc();

        DateTime cur = DateTime.utc(s.year, s.month, s.day);
        DateTime last = DateTime.utc(e.year, e.month, e.day);

        if (last.isBefore(cur)) continue;

        while (!cur.isAfter(last)) {
          out.add(cur);
          cur = cur.add(const Duration(days: 1));
        }
      } catch (ex) {
        // ignore parse errors
      }
    }
    return out;
  }

  DateTime? _findFirstAvailable() {
    final today = DateTime.now().toUtc().add(const Duration(days: 1));
    for (int i = 0; i < 365; i++) {
      final d = DateTime.utc(today.year, today.month, today.day).add(Duration(days: i));
      if (!blockedDays.contains(d)) return d;
    }
    return null;
  }

  void _openChatWithOwner() {
    final ownerId = item!['ownerId']?.toString() ?? item!['OwnerId']?.toString();
    final ownerName = (item!['ownerName'] is String && (item!['ownerName'] as String).trim().isNotEmpty)
        ? item!['ownerName'] as String
        : (item!['OwnerName'] is String && (item!['OwnerName'] as String).trim().isNotEmpty)
        ? item!['OwnerName'] as String
        : 'Seller';

    if (ownerId == null || ownerId.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Cannot contact seller - owner info not available')),
      );
      return;
    }

    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => ChatPage(
          otherUserId: ownerId,
          otherUserName: ownerName,
        ),
      ),
    );
  }

  Future<void> _showCalendarDialog() async {
    final result = await showDialog<DateTimeRange?>(
      context: context,
      builder: (ctx) {
        return _CalendarRangeDialog(
          blockedDays: blockedDays,
          initialStart: _selectedStartDate,
          initialEnd: _selectedEndDate,
        );
      },
    );

    if (!mounted) return;
    if (result != null) {
      setState(() {
        _selectedStartDate = result.start;
        _selectedEndDate = result.end;
      });
    }
  }

  Future<void> _requestBooking() async {
    if (_selectedStartDate == null || _selectedEndDate == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please select a valid borrow range.')),
      );
      return;
    }

    setState(() => _isBookingLoading = true);

    final startIso = _selectedStartDate!.toUtc().toIso8601String();
    final endIso = _selectedEndDate!.toUtc().toIso8601String();

    final success = await ApiService.createBooking(itemId: widget.itemId, startDateIso: startIso, endDateIso: endIso);

    setState(() => _isBookingLoading = false);

    if (!mounted) return;

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Booking request created successfully!')));
      await _load();
    } else {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Failed to create booking. Check availability or try again.')));
    }
  }

  // Logic to handle Edit Item Action
  Future<void> _handleEditItem() async {
    // Navigăm către pagina de editare
    final bool? result = await Navigator.push(
      context,
      MaterialPageRoute(
        // Trimitem obiectul 'item' curent către pagina de editare
        builder: (_) => EditItemPage(item: item!),
      ),
    );

    // Dacă editarea a avut succes (result == true), reîncărcăm datele
    if (result == true) {
      _load();
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Item updated successfully')),
      );
    }
  }

  Future<void> _showReportDialog(String itemTitle) async {
    await showDialog(
      context: context,
      builder: (context) => ReportItemDialog(
        itemId: widget.itemId,
        itemTitle: itemTitle,
      ),
    );
  }

  double get _averageReviewScore {
    if (_reviews.isEmpty) return 0;
    final ratings = _reviews.map((r) => r['rating'] as int? ?? 0).toList();
    if (ratings.isEmpty) return 0;
    return ratings.reduce((a, b) => a + b) / ratings.length;
  }

  Widget _buildReviewCard(Map<String, dynamic> review) {
    final rating = review['rating'] ?? 0;
    final comment = review['comment'] ?? '';
    final createdAt = review['createdAt'] != null
        ? DateTime.parse(review['createdAt'].toString()).toLocal()
        : null;
    final reviewerId = review['reviewerId']?.toString() ?? '';
    final isMyReview = _myReview != null && _myReview!['id']?.toString() == review['id']?.toString();

    return Container(
      key: _reviewKeys[review['id']?.toString() ?? ''],
      child: Card(
        margin: const EdgeInsets.only(bottom: 16),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        elevation: 2,
        color: isMyReview ? Colors.green.shade50 : null,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              FutureBuilder<Map<String, dynamic>>(
                future: reviewerId.isNotEmpty
                    ? ApiService.getUserById(reviewerId)
                    : Future.value({}),
                builder: (context, snapshot) {
                  final reviewerName = snapshot.hasData && snapshot.data!.isNotEmpty
                      ? '${snapshot.data!['firstName'] ?? ''} ${snapshot.data!['lastName'] ?? ''}'.trim()
                      : 'Anonymous';

                  return Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Row(
                        children: [
                          CircleAvatar(
                            radius: 18,
                            backgroundColor: Colors.deepPurple.shade100,
                            child: Text(
                              reviewerName.isNotEmpty ? reviewerName[0].toUpperCase() : 'A',
                              style: const TextStyle(color: Colors.deepPurple, fontWeight: FontWeight.bold),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Text(
                            reviewerName.isNotEmpty ? reviewerName : 'Anonymous',
                            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                          ),
                        ],
                      ),
                      Row(
                        children: [
                          if (createdAt != null)
                            Text(
                              DateFormat.yMMMd().format(createdAt),
                              style: TextStyle(color: Colors.grey[600], fontSize: 12),
                            ),
                          if (isMyReview) ...[
                            const SizedBox(width: 8),
                            IconButton(
                              icon: const Icon(Icons.edit, size: 18, color: Colors.deepPurple),
                              onPressed: () => _showEditReviewDialog(review),
                              tooltip: 'Edit review',
                              padding: EdgeInsets.zero,
                              constraints: const BoxConstraints(),
                            ),
                            IconButton(
                              icon: const Icon(Icons.delete, size: 18, color: Colors.red),
                              onPressed: () async {
                                final confirm = await showDialog<bool>(
                                  context: context,
                                  builder: (ctx) => AlertDialog(
                                    title: const Text('Delete Review'),
                                    content: const Text('Are you sure you want to delete your review?'),
                                    actions: [
                                      TextButton(
                                        onPressed: () => Navigator.of(ctx).pop(false),
                                        child: const Text('Cancel'),
                                      ),
                                      ElevatedButton(
                                        onPressed: () => Navigator.of(ctx).pop(true),
                                        child: const Text('Delete'),
                                      ),
                                    ],
                                  ),
                                );
                                if (confirm == true) {
                                  final resp = await ApiService.deleteReview(reviewId: review['id'].toString());
                                  if (resp['success'] == true) {
                                    setState(() {
                                      _reviews.removeWhere((r) => r['id'].toString() == review['id'].toString());
                                      if (_myReview != null && _myReview!['id'].toString() == review['id'].toString()) {
                                        _myReview = null;
                                        _hasUserReview = false;
                                      }
                                    });
                                    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Review deleted successfully.')));
                                  } else {
                                    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Failed to delete review.')));
                                  }
                                }
                              },
                              tooltip: 'Delete review',
                              padding: EdgeInsets.zero,
                              constraints: const BoxConstraints(),
                            ),
                          ],
                        ],
                      ),
                    ],
                  );
                },
              ),

              const SizedBox(height: 12),
              Row(
                children: List.generate(5, (index) {
                  return Icon(
                    index < rating ? Icons.star : Icons.star_border,
                    color: Colors.amber,
                    size: 18,
                  );
                }),
              ),

              if (comment.isNotEmpty) ...[
                const SizedBox(height: 10),
                Text(
                  comment,
                  style: TextStyle(color: Colors.grey[800], fontSize: 14),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _showAddReviewDialog() async {
    final TextEditingController commentController = TextEditingController();
    int selectedRating = 0;

    showDialog(
      context: context,
      builder: (BuildContext dialogContext) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              title: const Text('Write a Review'),
              content: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: List.generate(5, (index) {
                      return IconButton(
                        icon: Icon(
                          index < selectedRating ? Icons.star : Icons.star_border,
                          color: Colors.amber,
                          size: 30,
                        ),
                        onPressed: () {
                          setDialogState(() {
                            selectedRating = index + 1;
                          });
                        },
                      );
                    }),
                  ),
                  const SizedBox(height: 10),
                  TextField(
                    controller: commentController,
                    decoration: const InputDecoration(
                      border: OutlineInputBorder(),
                      hintText: 'Enter your review here...',
                    ),
                    maxLines: 3,
                  ),
                ],
              ),
              actions: [
                TextButton(
                  onPressed: () {
                    Navigator.of(dialogContext).pop();
                  },
                  child: const Text('Cancel'),
                ),
                ElevatedButton(
                  onPressed: () async {
                    if (commentController.text.trim().isEmpty || selectedRating <= 0) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('Please enter a review and select a rating.')),
                      );
                      return;
                    }

                    final token = await SecureStorageService.getAccessToken();
                    if (token == null || token.isEmpty) {
                      if (!context.mounted) return;
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('You must be logged in to leave a review.')),
                      );
                      return;
                    }

                    try {
                      final myBookings = await ApiService.getMyBookings();
                      final booking = myBookings.firstWhere((b) => (b['itemId']?.toString() ?? '') == widget.itemId, orElse: () => {});
                      if (booking.isEmpty) {
                        if (!context.mounted) return;
                        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('You need to have a booking for this item before leaving a review.')));
                        return;
                      }

                      final bookingId = booking['id']?.toString() ?? '';
                      if (bookingId.isEmpty) {
                        if (!context.mounted) return;
                        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Could not determine booking to attach review to.')));
                        return;
                      }

                      final resp = await ApiService.createReview(
                        bookingId: bookingId,
                        targetItemId: widget.itemId,
                        rating: selectedRating,
                        comment: commentController.text.trim(),
                      );

                      if (!context.mounted) return;

                      final bool ok = resp['success'] == true;
                      final int status = resp['status'] is int ? resp['status'] as int : int.tryParse(resp['status']?.toString() ?? '') ?? 0;
                      final String body = resp['body']?.toString() ?? '';

                      if (ok) {
                        Map<String, dynamic>? created;
                        try {
                          created = jsonDecode(body) as Map<String, dynamic>?;
                        } catch (_) {
                          created = null;
                        }

                        if (created != null) {
                          final createdNonNull = created!;
                          setState(() {
                            _reviews.insert(0, createdNonNull);
                            _myReview = createdNonNull;
                            _hasUserReview = true;
                            _reviewKeys[createdNonNull['id']?.toString() ?? ''] = GlobalKey();
                          });

                          Navigator.of(dialogContext).pop();
                          if (_reviewsSectionKey.currentContext != null) {
                            await Future.delayed(const Duration(milliseconds: 50));
                            Scrollable.ensureVisible(
                              _reviewsSectionKey.currentContext!,
                              duration: const Duration(milliseconds: 300),
                              curve: Curves.easeInOut,
                            );
                          }
                        } else {
                          await _loadReviews();
                          setState(() { _hasUserReview = true; });
                          Navigator.of(dialogContext).pop();
                          if (_reviewsSectionKey.currentContext != null) {
                            await Future.delayed(const Duration(milliseconds: 50));
                            Scrollable.ensureVisible(
                              _reviewsSectionKey.currentContext!,
                              duration: const Duration(milliseconds: 300),
                              curve: Curves.easeInOut,
                            );
                          }
                        }
                      } else {
                        String message = 'Failed to add review. (${status})';
                        if (status == 401) message = 'You must be logged in to perform this action.';
                        if (status == 403) message = 'You are not allowed to add a review (verify email or permissions).';
                        if (body.isNotEmpty) message += '\nServer: ${body}';
                        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
                      }
                    } catch (e) {
                      if (!context.mounted) return;
                      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error while adding review: $e')));
                    }
                  },
                  child: const Text('Submit'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  Future<void> _showEditReviewDialog(Map<String, dynamic> review) async {
    final TextEditingController commentController = TextEditingController(text: review['comment']);
    int selectedRating = review['rating'] ?? 0;

    showDialog(
      context: context,
      builder: (BuildContext dialogContext) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              title: const Text('Edit Review'),
              content: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: List.generate(5, (index) {
                      return IconButton(
                        icon: Icon(
                          index < selectedRating ? Icons.star : Icons.star_border,
                          color: Colors.amber,
                          size: 30,
                        ),
                        onPressed: () {
                          setDialogState(() {
                            selectedRating = index + 1;
                          });
                        },
                      );
                    }),
                  ),
                  const SizedBox(height: 10),
                  TextField(
                    controller: commentController,
                    decoration: const InputDecoration(
                      border: OutlineInputBorder(),
                      hintText: 'Edit your review here...',
                    ),
                    maxLines: 3,
                  ),
                ],
              ),
              actions: [
                TextButton(
                  onPressed: () {
                    Navigator.of(dialogContext).pop();
                  },
                  child: const Text('Cancel'),
                ),
                ElevatedButton(
                  onPressed: () async {
                    if (commentController.text.trim().isEmpty || selectedRating <= 0) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('Please enter a review and select a rating.')),
                      );
                      return;
                    }

                    final token = await SecureStorageService.getAccessToken();
                    if (token == null || token.isEmpty) {
                      if (!context.mounted) return;
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('You must be logged in to edit a review.')),
                      );
                      return;
                    }

                    try {
                      final resp = await ApiService.updateReview(
                        reviewId: review['id']?.toString() ?? '',
                        bookingId: review['bookingId']?.toString() ?? '',
                        targetUserId: review['targetUserId']?.toString(),
                        targetItemId: review['targetItemId']?.toString(),
                        rating: selectedRating,
                        comment: commentController.text.trim(),
                      );

                      if (!context.mounted) return;

                      final bool ok = resp['success'] == true;
                      final int status = resp['status'] is int ? resp['status'] as int : int.tryParse(resp['status']?.toString() ?? '') ?? 0;
                      final String body = resp['body']?.toString() ?? '';

                      if (ok) {
                        setState(() {
                          review['rating'] = selectedRating;
                          review['comment'] = commentController.text.trim();
                        });

                        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Review updated successfully!')));
                        Navigator.of(dialogContext).pop();
                      } else {
                        String message = 'Failed to update review. (${status})';
                        if (status == 401) message = 'You must be logged in to perform this action.';
                        if (status == 403) message = 'You are not allowed to update this review (verify email or permissions).';
                        if (body.isNotEmpty) message += '\nServer: ${body}';
                        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
                      }
                    } catch (e) {
                      if (!context.mounted) return;
                      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error while updating review: $e')));
                    }
                  },
                  child: const Text('Update'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    if (loading) return const Scaffold(body: Center(child: CircularProgressIndicator()));
    if (error != null) return Scaffold(body: Center(child: Text('Error: $error')));
    if (item == null) return const Scaffold(body: Center(child: Text('Item not found')));

    final imageUrl = item!['imageUrl'] as String?;
    final name = item!['name'] ?? 'No name';
    final description = item!['description'] ?? 'No description';
    final category = _mapIntOrStringToName(item!['category'], _categoryMap, 'N/A');
    final condition = _mapIntOrStringToName(item!['condition'], _conditionMap, 'N/A');
    final isAvailableOverall = !(item!['isAvailable'] == false);

    final ownerName = (item!['ownerName'] is String && (item!['ownerName'] as String).trim().isNotEmpty)
        ? item!['ownerName'] as String
        : (item!['ownerId'] != null ? (item!['ownerId'].toString()) : 'Unknown');

    // Logic to determine if current user is owner
    final rawOwnerId = item!['ownerId']?.toString() ?? item!['OwnerId']?.toString();

    // Normalize logic (trim and lowerCase) for robust comparison
    final normalizedOwnerId = rawOwnerId?.trim().toLowerCase();
    final normalizedUserId = _currentUserId?.trim().toLowerCase();

    // Print for debugging why it might fail
    print('--------------------------------------------------');
    print('DEBUG: Ownership Check');
    print('DEBUG: Raw Item Owner ID: "$rawOwnerId"');
    print('DEBUG: Raw Current User ID: "$_currentUserId"');
    print('DEBUG: Normalized Owner: "$normalizedOwnerId"');
    print('DEBUG: Normalized User:  "$normalizedUserId"');
    print('DEBUG: Match? ${normalizedOwnerId == normalizedUserId}');
    print('--------------------------------------------------');

    final bool isOwner = normalizedOwnerId != null &&
        normalizedUserId != null &&
        normalizedOwnerId == normalizedUserId;

    final recommended = _findFirstAvailable();
    final recommendedText = recommended != null ? DateFormat.yMMMd().format(recommended.toLocal()) : 'No availability soon';

    final String selectedRangeText = (_selectedStartDate != null && _selectedEndDate != null)
        ? '${DateFormat.yMMMd().format(_selectedStartDate!.toLocal())} → ${DateFormat.yMMMd().format(_selectedEndDate!.toLocal())}'
        : 'Tap to select range';

    final bool isLargeScreen = MediaQuery.of(context).size.width > 800;

    final Widget detailsColumn = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(name, style: TextStyle(fontSize: isLargeScreen ? 32 : 24, fontWeight: FontWeight.bold)),
        const SizedBox(height: 8),

        if (_acceptedReportsCount > 0)
          Container(
            margin: const EdgeInsets.only(bottom: 12),
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Colors.orange.shade50,
              border: Border.all(color: Colors.orange.shade300, width: 1.5),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Row(
              children: [
                Icon(Icons.warning_amber_rounded, color: Colors.orange.shade700, size: 24),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    'This product has $_acceptedReportsCount ${_acceptedReportsCount == 1 ? 'report' : 'reports'} in the last 7 days',
                    style: TextStyle(
                      color: Colors.orange.shade900,
                      fontWeight: FontWeight.w600,
                      fontSize: 14,
                    ),
                  ),
                ),
              ],
            ),
          ),

        if (_reviews.isNotEmpty)
          Row(
            children: [
              ...List.generate(5, (i) {
                final filled = i < _averageReviewScore.floor();
                return Icon(
                  filled ? Icons.star : Icons.star_border,
                  color: Colors.amber,
                  size: 28,
                );
              }),
              const SizedBox(width: 8),
              Text(_averageReviewScore.toStringAsFixed(1), style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
            ],
          ),
        const SizedBox(height: 8),
        Wrap(
          spacing: 10,
          runSpacing: 5,
          children: [
            Chip(label: Text(category, style: TextStyle(color: Colors.blue.shade800)), backgroundColor: Colors.blue.shade50),
            Chip(label: Text(condition), backgroundColor: Colors.amber.shade50, labelStyle: TextStyle(color: Colors.amber.shade800)),
          ],
        ),
        const SizedBox(height: 15),

        Row(
          children: [
            const Icon(Icons.person, size: 20, color: Colors.deepPurple),
            const SizedBox(width: 5),
            Text('Owner: $ownerName ${isOwner ? "(You)" : ""}', style: const TextStyle(color: Colors.deepPurple, fontWeight: FontWeight.w600)),
            const SizedBox(width: 16),
            Builder(
              builder: (context) {
                if (rawOwnerId == null) {
                  return Tooltip(
                    message: 'ownerId not available from API',
                    child: ElevatedButton.icon(
                      onPressed: null,
                      icon: const Icon(Icons.message, size: 18),
                      label: const Text('Contact Seller'),
                    ),
                  );
                }

                if (!isOwner) {
                  return ElevatedButton.icon(
                    onPressed: () => _openChatWithOwner(),
                    icon: const Icon(Icons.message, size: 18),
                    label: const Text('Contact Seller'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.deepPurple,
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                    ),
                  );
                }
                return const SizedBox.shrink();
              },
            ),
          ],
        ),
        const SizedBox(height: 15),

        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: Colors.deepPurple.shade50,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: Colors.deepPurple.shade100),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.check_circle_outline, size: 20, color: Colors.green),
                  const SizedBox(width: 8),
                  Text('First availability: $recommendedText', style: const TextStyle(fontWeight: FontWeight.bold)),
                ],
              ),
              const Divider(height: 20),

              OutlinedButton.icon(
                onPressed: _showCalendarDialog,
                icon: const Icon(Icons.date_range_outlined),
                label: Text(selectedRangeText),
                style: OutlinedButton.styleFrom(
                    foregroundColor: Colors.deepPurple,
                    side: const BorderSide(color: Colors.deepPurple, width: 1.5),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                    minimumSize: const Size(double.infinity, 45)
                ),
              ),
              if (!isAvailableOverall)
                const Padding(
                  padding: EdgeInsets.only(top: 8.0),
                  child: Text('Note: Item marked as generally unavailable by owner.', style: TextStyle(color: Colors.red, fontSize: 13)),
                ),
            ],
          ),
        ),

        const SizedBox(height: 30),

        Container(
          key: _reviewsSectionKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Reviews', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
              const SizedBox(height: 8),
              if (_reviews.isNotEmpty)
                Row(
                  children: [
                    ...List.generate(5, (i) {
                      final filled = i < _averageReviewScore.floor();
                      return Icon(
                        filled ? Icons.star : Icons.star_border,
                        color: Colors.amber,
                        size: 28,
                      );
                    }),
                    const SizedBox(width: 8),
                    Text(_averageReviewScore.toStringAsFixed(1), style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
                  ],
                ),
              const SizedBox(height: 15),
              if (_reviews.isEmpty)
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: Colors.grey.shade100,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Center(
                    child: Text('No reviews yet. Be the first to review this item!', style: TextStyle(color: Colors.grey)),
                  ),
                )
              else
                ListView.builder(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: _reviews.length,
                  itemBuilder: (context, index) {
                    final review = _reviews[index];
                    return _buildReviewCard(review);
                  },
                ),
            ],
          ),
        ),
        const SizedBox(height: 20),

        if (!_hasUserReview && !isOwner)
          OutlinedButton.icon(
            onPressed: _showAddReviewDialog,
            icon: const Icon(Icons.rate_review),
            label: const Text('Write a Review'),
            style: OutlinedButton.styleFrom(
              foregroundColor: Colors.deepPurple,
              side: const BorderSide(color: Colors.deepPurple, width: 1.5),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
              minimumSize: const Size(double.infinity, 45),
            ),
          )
        else if (_hasUserReview)
          ElevatedButton.icon(
            onPressed: () async {
              if (_myReview != null) {
                final id = _myReview!['id']?.toString();
                if (id != null && _reviewKeys.containsKey(id) && _reviewKeys[id]!.currentContext != null) {
                  await Scrollable.ensureVisible(
                    _reviewKeys[id]!.currentContext!,
                    duration: const Duration(milliseconds: 400),
                    curve: Curves.easeInOut,
                  );
                } else if (_reviewsSectionKey.currentContext != null) {
                  await Scrollable.ensureVisible(
                    _reviewsSectionKey.currentContext!,
                    duration: const Duration(milliseconds: 300),
                    curve: Curves.easeInOut,
                  );
                }
              }
            },
            icon: const Icon(Icons.visibility),
            label: const Text('View your review'),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.green.shade600,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
              minimumSize: const Size(double.infinity, 45),
            ),
          ),

        const SizedBox(height: 80),
      ],
    );

    final Widget imageWidget = Padding(
      padding: EdgeInsets.only(right: isLargeScreen ? 20.0 : 0, bottom: isLargeScreen ? 0 : 20),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(15),
        child: SizedBox(
          height: isLargeScreen ? 500 : 260,
          width: isLargeScreen ? 400 : double.infinity,
          child: imageUrl != null && imageUrl.isNotEmpty
              ? Image.network(
            imageUrl,
            fit: BoxFit.cover,
            errorBuilder: (context, error, stackTrace) => Container(
              color: Colors.grey[200],
              child: const Center(child: Icon(Icons.broken_image, size: 80, color: Colors.grey)),
            ),
          )
              : Container(
            color: Colors.deepPurple.shade100,
            child: const Center(
              child: Icon(Icons.photo_library, size: 80, color: Colors.deepPurple),
            ),
          ),
        ),
      ),
    );

    return Scaffold(
      appBar: AppBar(
        title: Text(name, style: const TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: Colors.deepPurple,
        foregroundColor: Colors.white,
        elevation: 0,
        actions: [
          // Don't report your own item
          if (!isOwner)
            IconButton(
              icon: const Icon(Icons.flag),
              tooltip: 'Report Item',
              onPressed: () => _showReportDialog(name),
            ),
        ],
      ),

      floatingActionButtonLocation: FloatingActionButtonLocation.centerDocked,
      floatingActionButton: Padding(
        padding: const EdgeInsets.all(16.0),
        child: SizedBox(
          width: double.infinity,
          height: 50,
          child: isOwner
          // Button for Owner: EDIT ITEM
              ? ElevatedButton.icon(
            onPressed: _handleEditItem,
            icon: const Icon(Icons.edit),
            label: const Text(
              'Edit Item',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.blueAccent, // Distinct color for owner action
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              elevation: 8,
            ),
          )
          // Button for Others: REQUEST BORROWING
              : ElevatedButton.icon(
            onPressed: isAvailableOverall && !_isBookingLoading ? _requestBooking : null,
            icon: _isBookingLoading
                ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 3))
                : const Icon(Icons.calendar_month),
            label: Text(
              isAvailableOverall ? 'Request Borrowing' : 'Item is not generally available',
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.deepPurple,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              elevation: 8,
            ),
          ),
        ),
      ),

      body: SingleChildScrollView(
        child: Padding(
          padding: const EdgeInsets.all(20.0),
          child: isLargeScreen
              ? Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Flexible(
                flex: 4,
                child: imageWidget,
              ),
              Flexible(
                flex: 6,
                child: detailsColumn,
              ),
            ],
          )
              : Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              imageWidget,
              const SizedBox(height: 20),
              detailsColumn,
            ],
          ),
        ),
      ),
    );
  }
}

class _CalendarRangeDialog extends StatefulWidget {
  final Set<DateTime> blockedDays;
  final DateTime? initialStart;
  final DateTime? initialEnd;

  const _CalendarRangeDialog({Key? key, required this.blockedDays, this.initialStart, this.initialEnd}) : super(key: key);

  @override
  State<_CalendarRangeDialog> createState() => _CalendarRangeDialogState();
}

class _CalendarRangeDialogState extends State<_CalendarRangeDialog> {
  late DateTime _firstMonth;
  DateTime? _rangeStart;
  DateTime? _rangeEnd;

  @override
  void initState() {
    super.initState();
    _rangeStart = widget.initialStart;
    _rangeEnd = widget.initialEnd;
    _firstMonth = widget.initialStart ?? DateTime.now();
  }

  bool _isBlocked(DateTime day) {
    final key = DateTime.utc(day.year, day.month, day.day);
    return widget.blockedDays.contains(key);
  }

  bool _rangeOverlapsBlocked(DateTime start, DateTime end) {
    DateTime cur = DateTime.utc(start.year, start.month, start.day);
    final last = DateTime.utc(end.year, end.month, end.day);
    while (!cur.isAfter(last)) {
      if (_isBlocked(cur)) return true;
      cur = cur.add(const Duration(days: 1));
    }
    return false;
  }

  void _previousMonth() {
    setState(() {
      _firstMonth = DateTime(_firstMonth.year, _firstMonth.month - 1);
    });
  }

  void _nextMonth() {
    setState(() {
      _firstMonth = DateTime(_firstMonth.year, _firstMonth.month + 1);
    });
  }

  void _onDaySelected(DateTime selected) {
    if (_isBlocked(selected)) return;
    setState(() {
      if (_rangeStart == null || (_rangeStart != null && _rangeEnd != null)) {
        _rangeStart = selected;
        _rangeEnd = null;
      } else if (_rangeStart != null && _rangeEnd == null) {
        if (selected.isBefore(_rangeStart!)) {
          _rangeEnd = _rangeStart;
          _rangeStart = selected;
        } else {
          _rangeEnd = selected;
        }
        if (_rangeEnd != null && _rangeOverlapsBlocked(_rangeStart!, _rangeEnd!)) {
          ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Selected range overlaps unavailable days')));
          _rangeEnd = null;
        }
      }
    });
  }

  Widget _buildMonthCalendar(DateTime monthToShow) {
    return TableCalendar(
      firstDay: DateTime.now().subtract(const Duration(days: 365)),
      lastDay: DateTime.now().add(const Duration(days: 365)),
      focusedDay: monthToShow,
      calendarFormat: CalendarFormat.month,
      headerVisible: false,
      selectedDayPredicate: (day) =>
      (_rangeStart != null && isSameDay(day, _rangeStart!)) ||
          (_rangeEnd != null && isSameDay(day, _rangeEnd!)),
      rangeSelectionMode: RangeSelectionMode.toggledOn,
      rangeStartDay: _rangeStart,
      rangeEndDay: _rangeEnd,
      onDaySelected: (selected, focused) => _onDaySelected(selected),
      availableCalendarFormats: const {CalendarFormat.month: 'Month'},
      calendarStyle: CalendarStyle(
        todayDecoration: BoxDecoration(
          border: Border.all(color: Colors.deepPurple, width: 2),
          shape: BoxShape.rectangle,
          borderRadius: BorderRadius.circular(8),
        ),
        todayTextStyle: const TextStyle(color: Colors.black, fontWeight: FontWeight.bold),
        selectedDecoration: BoxDecoration(
          color: Colors.deepPurple,
          shape: BoxShape.rectangle,
          borderRadius: BorderRadius.circular(8),
        ),
        selectedTextStyle: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
        rangeStartDecoration: BoxDecoration(
          color: Colors.deepPurple,
          shape: BoxShape.rectangle,
          borderRadius: BorderRadius.circular(8),
        ),
        rangeEndDecoration: BoxDecoration(
          color: Colors.deepPurple,
          shape: BoxShape.rectangle,
          borderRadius: BorderRadius.circular(8),
        ),
        rangeHighlightColor: Colors.deepPurple.shade100,
        outsideDaysVisible: false,
      ),
      daysOfWeekStyle: DaysOfWeekStyle(
        weekdayStyle: TextStyle(color: Colors.grey.shade600, fontWeight: FontWeight.w600),
        weekendStyle: TextStyle(color: Colors.grey.shade600, fontWeight: FontWeight.w600),
      ),
      calendarBuilders: CalendarBuilders(
        defaultBuilder: (context, day, focusedDay) {
          final blocked = _isBlocked(day);
          // Modificat pentru a bloca doar zilele strict înainte de ziua curentă (ora 00:00)
          final now = DateTime.now();
          final today = DateTime(now.year, now.month, now.day);
          final bool isPast = day.isBefore(today);
          return Center(
            child: Container(
              decoration: BoxDecoration(
                color: blocked || isPast ? Colors.grey.shade100 : Colors.white,
                borderRadius: BorderRadius.circular(8),
              ),
              padding: const EdgeInsets.all(8),
              child: Text(
                '${day.day}',
                style: TextStyle(
                  color: blocked || isPast ? Colors.grey.shade400 : Colors.black,
                  decoration: blocked ? TextDecoration.lineThrough : null,
                  fontWeight: FontWeight.normal,
                ),
              ),
            ),
          );
        },
        selectedBuilder: (context, day, focusedDay) {
          return Center(
            child: Container(
              decoration: BoxDecoration(
                color: Colors.deepPurple,
                borderRadius: BorderRadius.circular(8),
              ),
              padding: const EdgeInsets.all(8),
              child: Text(
                '${day.day}',
                style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
              ),
            ),
          );
        },
        rangeStartBuilder: (context, day, focusedDay) {
          return Center(
            child: Container(
              decoration: BoxDecoration(
                color: Colors.deepPurple,
                borderRadius: BorderRadius.circular(8),
              ),
              padding: const EdgeInsets.all(8),
              child: Text(
                '${day.day}',
                style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
              ),
            ),
          );
        },
        rangeEndBuilder: (context, day, focusedDay) {
          return Center(
            child: Container(
              decoration: BoxDecoration(
                color: Colors.deepPurple,
                borderRadius: BorderRadius.circular(8),
              ),
              padding: const EdgeInsets.all(8),
              child: Text(
                '${day.day}',
                style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
              ),
            ),
          );
        },
        rangeHighlightBuilder: (context, day, focusedDay) {
          return Center(
            child: Container(
              decoration: BoxDecoration(
                color: Colors.deepPurple.shade100,
                borderRadius: BorderRadius.circular(8),
              ),
              padding: const EdgeInsets.all(8),
              child: Text(
                '${day.day}',
                style: const TextStyle(color: Colors.black, fontWeight: FontWeight.bold),
              ),
            ),
          );
        },
        outsideBuilder: (context, day, focusedDay) => const SizedBox.shrink(),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final secondMonth = DateTime(_firstMonth.year, _firstMonth.month + 1);
    final firstMonthName = DateFormat.yMMMM().format(_firstMonth);
    final secondMonthName = DateFormat.yMMMM().format(secondMonth);

    final double dialogWidth = MediaQuery.of(context).size.width > 950 ? 900 : 700;

    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Container(
        padding: const EdgeInsets.all(24),
        width: dialogWidth,
        constraints: const BoxConstraints(maxHeight: 550),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                IconButton(
                  icon: const Icon(Icons.chevron_left, size: 32, color: Colors.deepPurple),
                  onPressed: _previousMonth,
                ),
                Expanded(
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                    children: [
                      Text(firstMonthName, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Colors.deepPurple)),
                      Text(secondMonthName, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Colors.deepPurple)),
                    ],
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.chevron_right, size: 32, color: Colors.deepPurple),
                  onPressed: _nextMonth,
                ),
              ],
            ),
            const SizedBox(height: 16),
            Expanded(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(child: _buildMonthCalendar(_firstMonth)),
                  const SizedBox(width: 20),
                  Expanded(child: _buildMonthCalendar(secondMonth)),
                ],
              ),
            ),
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  _rangeStart == null
                      ? 'Start Date: -'
                      : 'Start: ${DateFormat.yMMMd().format(_rangeStart!.toLocal())}',
                  style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
                ),
                const SizedBox(width: 20),
                Text(
                  _rangeEnd == null
                      ? 'End Date: -'
                      : 'End: ${DateFormat.yMMMd().format(_rangeEnd!.toLocal())}',
                  style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
                ),
              ],
            ),
            const SizedBox(height: 20),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: () => Navigator.of(context).pop(null),
                  child: const Text('Cancel', style: TextStyle(color: Colors.deepPurple)),
                ),
                const SizedBox(width: 12),
                ElevatedButton(
                  onPressed: (_rangeStart != null && _rangeEnd != null) ? () {
                    Navigator.of(context).pop(DateTimeRange(start: _rangeStart!, end: _rangeEnd!));
                  } : null,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.deepPurple,
                    foregroundColor: Colors.white,
                  ),
                  child: const Text('Confirm Selection'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}