import 'package:flutter/material.dart';
import '../models/conversation.dart';
import '../services/chat_service.dart';
import '../services/api_service.dart';
import '../services/secure_storage_service.dart';
import 'chat_page.dart';

class ConversationsPage extends StatefulWidget {
  const ConversationsPage({super.key});

  @override
  State<ConversationsPage> createState() => _ConversationsPageState();
}

class _ConversationsPageState extends State<ConversationsPage> {
  List<Conversation> _conversations = [];
  bool _isLoading = true;
  String? _currentUserId;

  @override
  void initState() {
    super.initState();
    _loadConversations();
    _setupMessageListener();
  }

  @override
  void dispose() {
    ChatService.removeMessageListener(_onNewMessage);
    super.dispose();
  }

  void _setupMessageListener() {
    ChatService.addMessageListener(_onNewMessage);
  }

  void _onNewMessage(Map<String, dynamic> message) {
    // Refresh conversations when a new message arrives
    _loadConversations();
  }

  Future<void> _loadConversations() async {
    setState(() => _isLoading = true);

    try {
      final token = await SecureStorageService.getAccessToken();
      _currentUserId = ApiService.getUserIdFromToken(token);

      final data = await ChatService.getConversations();
      setState(() {
        _conversations = data.map((e) => Conversation.fromJson(e)).toList();
        _isLoading = false;
      });
    } catch (e) {
      print('❌ Error loading conversations: $e');
      setState(() => _isLoading = false);
    }
  }

  void _openChat(Conversation conversation) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => ChatPage(
          otherUserId: conversation.userId,
          otherUserName: conversation.userName,
        ),
      ),
    ).then((_) => _loadConversations()); // Refresh on return
  }

  String _formatTime(DateTime? time) {
    if (time == null) return '';
    final now = DateTime.now();
    final diff = now.difference(time);

    if (diff.inDays > 0) {
      return '${diff.inDays}d ago';
    } else if (diff.inHours > 0) {
      return '${diff.inHours}h ago';
    } else if (diff.inMinutes > 0) {
      return '${diff.inMinutes}m ago';
    } else {
      return 'Just now';
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Messages'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadConversations,
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _conversations.isEmpty
              ? _buildEmptyState()
              : RefreshIndicator(
                  onRefresh: _loadConversations,
                  child: ListView.separated(
                    itemCount: _conversations.length,
                    separatorBuilder: (_, __) => const Divider(height: 1),
                    itemBuilder: (context, index) {
                      final conversation = _conversations[index];
                      return _buildConversationTile(conversation);
                    },
                  ),
                ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.chat_bubble_outline,
            size: 80,
            color: Colors.grey[400],
          ),
          const SizedBox(height: 16),
          Text(
            'No conversations yet',
            style: TextStyle(
              fontSize: 18,
              color: Colors.grey[600],
              fontWeight: FontWeight.w500,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            'Start a conversation from an item page',
            style: TextStyle(
              fontSize: 14,
              color: Colors.grey[500],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildConversationTile(Conversation conversation) {
    final isMyLastMessage = conversation.lastMessageSenderId == _currentUserId;

    return ListTile(
      leading: CircleAvatar(
        backgroundColor: Theme.of(context).primaryColor,
        child: Text(
          conversation.userName.isNotEmpty
              ? conversation.userName[0].toUpperCase()
              : '?',
          style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
        ),
      ),
      title: Text(
        conversation.userName,
        style: const TextStyle(fontWeight: FontWeight.w600),
      ),
      subtitle: conversation.lastMessage != null
          ? Row(
              children: [
                if (isMyLastMessage)
                  const Text(
                    'You: ',
                    style: TextStyle(fontWeight: FontWeight.w500),
                  ),
                Expanded(
                  child: Text(
                    conversation.lastMessage!,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            )
          : null,
      trailing: conversation.lastMessageTime != null
          ? Text(
              _formatTime(conversation.lastMessageTime),
              style: TextStyle(
                fontSize: 12,
                color: Colors.grey[500],
              ),
            )
          : null,
      onTap: () => _openChat(conversation),
    );
  }
}

