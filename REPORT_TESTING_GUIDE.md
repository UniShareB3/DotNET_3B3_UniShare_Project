# 🧪 Testing Instructions - Report Functionality

## Prerequisites

1. Backend server running on `http://localhost:5083` (or configured API_BASE_URL)
2. Frontend Flutter app running
3. Test accounts:
   - **Regular User**: Any registered user
   - **Moderator/Admin**: User with Admin or Moderator role

---

## Test Scenarios

### 1. ✅ User Can Report an Item

**Steps:**
1. Login as a regular user
2. Navigate to any item page (browse items from home page)
3. Click the **Flag icon** (🚩) in the AppBar
4. In the dialog that opens:
   - Verify item title is displayed
   - Enter description: "Test report - inappropriate content"
   - Click "Submit Report"
5. Verify success message appears

**Expected Results:**
- ✅ Dialog opens correctly
- ✅ Validation works (try submitting empty or <10 chars)
- ✅ Success SnackBar shows: "Report submitted successfully. A moderator will review it."
- ✅ Backend returns 201 Created
- ✅ Report is created in database

**Test Edge Cases:**
- Try reporting without being logged in (should show error)
- Try description with exactly 10 characters (should work)
- Try description with 1000+ characters (should be limited)
- Try special characters in description

---

### 2. ✅ Moderator Can View Reports

**Steps:**
1. Login as moderator/admin user
2. Open the drawer menu (hamburger icon ☰)
3. Verify "Moderator Reports" option is visible
4. Click on "Moderator Reports"
5. Verify reports page loads

**Expected Results:**
- ✅ Menu item only visible to moderators/admins
- ✅ Page loads without errors
- ✅ Reports assigned to this moderator are displayed
- ✅ Filter tabs show correct counts

**Test Edge Cases:**
- Login as regular user → Menu item should NOT appear
- New moderator with no reports → Empty state should show
- Moderator with reports → Cards display correctly

---

### 3. ✅ Filter Reports by Status

**Steps:**
1. On Moderator Reports page
2. Click each filter tab:
   - ALL
   - PENDING
   - ACCEPTED
   - DECLINED
3. Observe the list updates

**Expected Results:**
- ✅ ALL shows all reports
- ✅ PENDING shows only pending reports
- ✅ ACCEPTED shows only accepted reports
- ✅ DECLINED shows only declined reports
- ✅ Count badges are accurate

---

### 4. ✅ Accept a Report

**Steps:**
1. On Moderator Reports page
2. Filter to PENDING
3. Find a pending report
4. Click **"Accept"** button (green)
5. Confirm in dialog
6. Verify action completes

**Expected Results:**
- ✅ Confirmation dialog appears
- ✅ After confirmation, success SnackBar shows
- ✅ Report status changes to ACCEPTED
- ✅ Report moves to ACCEPTED filter
- ✅ Backend PATCH request succeeds
- ✅ Action buttons disappear for this report

**Test Edge Cases:**
- Cancel confirmation dialog → No change occurs
- Accept report → Refresh page → Status persists

---

### 5. ✅ Decline a Report

**Steps:**
1. On Moderator Reports page
2. Filter to PENDING
3. Find a pending report
4. Click **"Decline"** button (red)
5. Confirm in dialog
6. Verify action completes

**Expected Results:**
- ✅ Confirmation dialog appears
- ✅ After confirmation, success SnackBar shows
- ✅ Report status changes to DECLINED
- ✅ Report moves to DECLINED filter
- ✅ Backend PATCH request succeeds
- ✅ Action buttons disappear for this report

---

### 6. ✅ Report Card Display

**Verify each report card shows:**
- ✅ Status badge with correct color:
  - 🟠 Orange for PENDING
  - 🟢 Green for ACCEPTED
  - 🔴 Red for DECLINED
- ✅ Creation date formatted correctly (e.g., "Dec 17, 2024 14:30")
- ✅ Full description text
- ✅ Item ID (first 8 chars)
- ✅ Reporter ID (first 8 chars)
- ✅ Action buttons only for PENDING reports

---

### 7. ✅ Pull to Refresh

**Steps:**
1. On Moderator Reports page
2. Pull down to refresh (or wait for auto-refresh trigger)
3. Verify list updates

**Expected Results:**
- ✅ Loading indicator shows during refresh
- ✅ Report list updates with latest data
- ✅ Counts update in filter tabs

---

### 8. ✅ Error Handling

**Test scenarios:**
1. **No internet connection**:
   - Try creating report → Error message shows
   - Try loading reports page → Error state displays with retry button

2. **Invalid token/expired session**:
   - Backend should return 401
   - Frontend should show authentication error

3. **Backend validation errors**:
   - Try short description (< 10 chars) → Validation error
   - Backend should return 400 with error details

**Expected Results:**
- ✅ Errors are caught gracefully
- ✅ User-friendly error messages display
- ✅ Retry options available where appropriate
- ✅ No crashes or blank screens

---

## Backend Verification

### Check Report Creation:

```bash
# Get all reports (as admin)
curl -X GET http://localhost:5083/reports \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

### Check Moderator Assignment:

```sql
-- In database, verify moderatorId is set
SELECT * FROM Reports WHERE Status = 'PENDING';
```

### Check Load Balancing:

Create multiple reports and verify they're distributed among moderators.

---

## API Testing with Backend.http

If available, test endpoints directly:

```http
### Create Report
POST http://localhost:5083/reports
Authorization: Bearer {{userToken}}
Content-Type: application/json

{
  "itemId": "{{itemId}}",
  "userId": "{{userId}}",
  "description": "This item contains inappropriate content"
}

### Get Reports by Moderator
GET http://localhost:5083/reports/moderator/{{moderatorId}}
Authorization: Bearer {{moderatorToken}}

### Update Report Status
PATCH http://localhost:5083/reports/{{reportId}}
Authorization: Bearer {{moderatorToken}}
Content-Type: application/json

{
  "status": "ACCEPTED",
  "moderatorId": "{{moderatorId}}"
}
```

---

## Performance Testing

1. **Load Test**: Create 100+ reports, verify page loads quickly
2. **Filter Performance**: Switch between filters, verify instant response
3. **Concurrent Actions**: Multiple moderators acting on different reports

---

## Security Testing

1. ✅ Regular user cannot access moderator reports endpoint
2. ✅ User cannot update report status without admin/moderator role
3. ✅ User cannot see reports assigned to other moderators
4. ✅ SQL injection attempts in description field are sanitized
5. ✅ XSS attempts in description are escaped in UI

---

## Mobile/Responsive Testing

1. Test on mobile screen sizes
2. Verify dialog is responsive
3. Check touch interactions on cards
4. Ensure buttons are easily tappable

---

## Regression Testing

After implementing reports, verify:
- ✅ Other features still work (booking, reviews, etc.)
- ✅ Navigation is not broken
- ✅ Authentication flow unchanged
- ✅ Profile page works
- ✅ Item listing/details work

---

## Known Issues to Watch For

1. **Token Expiration**: Long-running sessions might expire mid-action
2. **Role Claim Format**: JWT role claim might vary by backend config
3. **Date Parsing**: Timezone differences might cause display issues
4. **Empty States**: Ensure proper messages for empty report lists

---

## Success Criteria

All tests pass when:
- ✅ Users can report items
- ✅ Moderators can view their reports
- ✅ Reports can be accepted/declined
- ✅ UI is responsive and user-friendly
- ✅ No errors in console
- ✅ Backend API calls succeed
- ✅ Security is maintained
- ✅ Performance is acceptable

---

## Test Data Setup

### Create Test Users:
1. User A: Regular user (reporter)
2. User B: Moderator
3. User C: Admin
4. User D: Another moderator (for load balancing test)

### Create Test Items:
1. Item 1: Normal item to be reported
2. Item 2: Another item for multiple reports test
3. Item 3: Item with existing reports

### Create Test Reports:
- 5 PENDING reports
- 3 ACCEPTED reports
- 2 DECLINED reports

---

## Debugging Tips

If issues occur:

1. **Check Console**: Look for network errors or exceptions
2. **Check Backend Logs**: Review Serilog output
3. **Verify Token**: Use jwt.io to decode and check claims
4. **Check API Responses**: Use browser DevTools Network tab
5. **Verify Database**: Check report records directly

---

Happy Testing! 🎉

