# 🧪 Testing Instructions for Mock Data System

## 🚀 **How to Test Phase B Implementation**

### **Step 1: Build the Project**
```bash
cd /Users/pranavpandey/AgenticMCP/src/AgenticOrderingSystem.API
dotnet build
```

**Expected Result:** ✅ Build should succeed without errors

### **Step 2: Run the Application**
```bash
dotnet run
```

**Expected Result:** 
```
✅ Loaded environment variables from: /Users/pranavpandey/AgenticMCP/src/AgenticOrderingSystem.API/../../.env
✅ Database connections initialized successfully
✅ ProductDesigner_DB connection successful
✅ CMP_DB connection successful
✅ Database connections verified successfully!
ℹ️ Now listening on: http://localhost:5001
```

### **Step 3: Test API Endpoints**

#### **Health Check**
```bash
curl http://localhost:5001/api/dev/health
```

**Expected Response:**
```json
{
  "databaseConnected": true,
  "timestamp": "2025-07-19T...",
  "seedStatus": {
    "categoriesCount": 0,
    "productsCount": 0,
    "usersCount": 0,
    "isSeeded": false
  },
  "environment": "Development"
}
```

#### **Seed Database with Mock Data**
```bash
curl -X POST http://localhost:5001/api/dev/seed
```

**Expected Response:**
```json
{
  "message": "Database seeded successfully",
  "seedStatus": {
    "categoriesCount": 5,
    "productsCount": 6,
    "usersCount": 12,
    "isSeeded": true
  }
}
```

#### **Verify Data Summary**
```bash
curl http://localhost:5001/api/dev/data-summary
```

**Expected Response:** Detailed summary showing:
- ✅ 5 Categories (Software, Hardware, Training, Services, Travel)
- ✅ 6 Products (Adobe Creative, Jira, Dell Laptop, Monitor, AWS Training, Security Consulting)
- ✅ 12 Users (1 MD, 2 Directors, 3 Managers, 5 Employees, 1 Admin)
- ✅ User Hierarchy with proper reporting structure

### **Step 4: Verify Data in MongoDB Atlas**

1. **Go to MongoDB Atlas Dashboard**
2. **Navigate to Browse Collections**
3. **Check ProductDesigner_DB:**
   - `products` collection should have 6 documents
   - `categories` collection should have 5 documents
4. **Check CMP_DB:**
   - `users` collection should have 12 documents

### **Step 5: Test Data Reset**
```bash
curl -X DELETE http://localhost:5001/api/dev/clear
```

**Expected Response:**
```json
{
  "message": "Database cleared successfully"
}
```

---

## 🔍 **What to Verify:**

### **Categories (5 total):**
- ✅ Software & Licenses (ID: cat_software)
- ✅ Hardware & Equipment (ID: cat_hardware)  
- ✅ Training & Development (ID: cat_training)
- ✅ Professional Services (ID: cat_services)
- ✅ Travel & Expenses (ID: cat_travel)

### **Products (6 total):**
- ✅ Adobe Creative Suite ($600) - Complex questions
- ✅ Atlassian Jira/Confluence ($1,200) - Team-based
- ✅ Dell XPS 15 Laptop ($2,500) - Hardware specs
- ✅ 34" Ultrawide Monitor ($800) - Simple request
- ✅ AWS Certification Training ($1,500) - Career development
- ✅ Security Consulting ($20,000) - High-value service

### **Users (12 total):**
- ✅ 1 Managing Director (Sarah Wilson) - $1M approval limit
- ✅ 2 Directors (James Chen - Engineering, Maria Garcia - Operations)
- ✅ 3 Managers (Alice, Bob, Carol) - Various approval limits
- ✅ 5 Employees (John, Jane, Mike, Lisa) - No approval authority
- ✅ 1 System Admin - Special permissions

### **Approval Hierarchies:**
- ✅ Software requests: Manager → Director (if >$5K)
- ✅ Hardware requests: Manager → Director (if >$3K)
- ✅ Training requests: Manager only (up to $5K)
- ✅ Services requests: Manager → MD (if >$10K)
- ✅ Travel requests: Manager → Director (if >$3K)

---

## 🚨 **If You Encounter Issues:**

### **Build Errors:**
- Check .NET 8 is installed: `dotnet --version`
- Ensure MongoDB packages are restored
- Verify environment variables in `.env` file

### **Database Connection Issues:**
- Verify MongoDB Atlas connection string
- Check network access settings in Atlas
- Ensure database user has proper permissions

### **API Errors:**
- Check application logs for detailed error messages
- Verify all required services are registered in DI container
- Ensure controllers and services have proper using statements

---

## ✅ **Success Criteria:**

**Phase B is successful when:**
1. ✅ Application builds without errors
2. ✅ Database connections are established
3. ✅ Mock data seeds successfully (5 categories, 6 products, 12 users)
4. ✅ API endpoints return expected data
5. ✅ User hierarchy displays properly
6. ✅ Approval models are correctly configured

**Ready for Phase A when all above criteria are met!** 🎯
