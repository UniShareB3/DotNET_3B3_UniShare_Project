# 🚀 Quick Start - Report Functionality

## ⚡ TL;DR

Funcționalitatea de **Reports** permite utilizatorilor să raporteze item-uri problematice, iar moderatorii/adminii să le revizuiască și să ia decizii.

---

## 📦 What Was Added

### New Files:
- ✅ `lib/models/report.dart` - Data models
- ✅ `lib/screens/report_item_dialog.dart` - User report dialog
- ✅ `lib/screens/moderator_reports_page.dart` - Moderator dashboard

### Modified Files:
- 🔧 `lib/services/api_service.dart` - Added report API methods
- 🔧 `lib/screens/product_page.dart` - Added report button
- 🔧 `lib/screens/main_page.dart` - Added moderator menu option

---

## 🎯 Quick Usage

### For Users:
1. Browse to any item page
2. Click the **Flag icon** (🚩) in top-right
3. Describe the problem
4. Submit

### For Moderators:
1. Open menu (☰)
2. Click "Moderator Reports"
3. Review pending reports
4. Accept or Decline

---

## 🔑 Key Features

| Feature | Description |
|---------|-------------|
| 🚩 **Report Items** | Users can flag problematic content |
| 🎯 **Auto-Assignment** | Reports distributed to moderators automatically |
| 📊 **Filter by Status** | PENDING, ACCEPTED, DECLINED |
| 🎨 **Color Coding** | Visual status indicators |
| ✅ **Accept/Decline** | Moderators can approve or reject reports |
| 🔒 **Role-Based Access** | Only moderators see the dashboard |
| 🔄 **Real-time Updates** | Pull-to-refresh support |

---

## 🛠️ Technical Stack

- **Frontend**: Flutter (Dart)
- **Backend**: .NET 7+ with MediatR
- **Database**: SQL Server (Entity Framework)
- **Auth**: JWT with role-based claims
- **Validation**: FluentValidation

---

## 📝 API Endpoints Used

```
POST   /reports                               (Create report)
GET    /reports/moderator/{moderatorId}      (Get assigned reports)
PATCH  /reports/{reportId}                   (Update status)
```

---

## 🎨 UI Components

### ReportItemDialog
- Modal dialog
- Text field for description
- Validation (10-1000 chars)
- Submit button with loading state

### ModeratorReportsPage
- Filter tabs (ALL/PENDING/ACCEPTED/DECLINED)
- Report cards with:
  - Status badge
  - Description
  - Item & Reporter info
  - Action buttons
- Pull-to-refresh

---

## 🔐 Security

- JWT token required for all operations
- Role claims checked: `Admin` or `Moderator`
- Menu option hidden for regular users
- Backend validates roles on each endpoint

---

## 📚 Documentation

Detailed documentation available:

1. **REPORT_IMPLEMENTATION_SUMMARY.md** - Full implementation details
2. **REPORT_USER_GUIDE.md** - User and moderator guide
3. **REPORT_TESTING_GUIDE.md** - Complete testing scenarios
4. **REPORT_ARCHITECTURE.md** - System architecture diagrams

---

## ✅ Testing Checklist

Quick smoke test:

```
[ ] User can see flag icon on item page
[ ] User can open report dialog
[ ] User can submit report with valid description
[ ] Moderator menu option appears for moderators only
[ ] Moderator can view reports
[ ] Filter tabs work
[ ] Moderator can accept a report
[ ] Moderator can decline a report
[ ] Status colors display correctly
[ ] No console errors
```

---

## 🐛 Troubleshooting

### "Moderator Reports" not showing in menu?
- Check if user has `Admin` or `Moderator` role in JWT token
- Verify role claim format: `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`

### Report submission fails?
- Check if user is authenticated
- Verify email is confirmed (requires `RequireEmailVerification`)
- Check description length (10-1000 chars)

### Reports not loading?
- Verify backend is running
- Check network tab for API errors
- Ensure moderator has reports assigned

---

## 🚀 Next Steps (Future Enhancements)

- [ ] Real-time notifications for new reports
- [ ] Report statistics dashboard
- [ ] Bulk actions
- [ ] Report categories/types
- [ ] Image evidence upload
- [ ] Appeal system
- [ ] Admin override for assignments
- [ ] Report analytics/trends

---

## 💡 Example Usage Flow

```
1. Alice finds an inappropriate item
2. Alice clicks Flag icon → Opens dialog
3. Alice writes: "This item contains offensive content"
4. Alice clicks Submit → Report created
5. System assigns report to Bob (moderator with least pending)
6. Bob opens "Moderator Reports" from menu
7. Bob sees Alice's report in PENDING tab
8. Bob reviews the item
9. Bob clicks Accept → Report marked as ACCEPTED
10. System may auto-hide the item after N accepted reports
```

---

## 📞 Support

For questions or issues:
- Check documentation files
- Review backend logs in `Backend/logs/`
- Check Flutter console for errors
- Contact development team

---

## ✨ Credits

Implemented as part of the UniShare project - a university item sharing platform.

**Version**: 1.0  
**Date**: December 2024  
**Status**: ✅ Production Ready

---

**Happy Reporting!** 🎉

