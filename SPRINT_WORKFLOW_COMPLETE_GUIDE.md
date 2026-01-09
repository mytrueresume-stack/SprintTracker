# Sprint Workflow Guide - Manager & Developer Flow

## Complete Sprint Lifecycle

### Phase 1: Sprint Creation & Planning (Manager)

#### Step 1: Create Sprint
**Manager Actions**:
1. Navigate to Project ? Sprints tab
2. Click "Create Sprint" button
3. Fill in sprint details:
   - Sprint Name (e.g., "Sprint 1", "January 2026 Sprint")
   - Sprint Goal (what you want to achieve)
   - Start Date
   - End Date (typically 2 weeks)
4. Click "Create Sprint"
5. Sprint is created in **Planning** status

**Result**: Sprint appears in project with yellow "Planning" badge

---

#### Step 2: Add Tasks to Sprint
**Manager Actions**:
1. Go to Backlog tab
2. Click "Add Task" to create new tasks
3. OR drag existing backlog tasks to the sprint
4. Assign tasks to team members
5. Set story points for each task

**Best Practices**:
- Assign tasks evenly across team members
- Include acceptance criteria
- Set realistic story points
- Ensure all team members have work

---

#### Step 3: Start Sprint
**Manager Actions**:
1. Go to Sprints tab
2. Find sprint in "Planning" status
3. Click three-dot menu (?)
4. Click "Start" button
5. Confirm sprint start

**What Happens**:
- ? Sprint status changes to **Active** (green badge with dot)
- ? Sprint appears in **ALL team members' dashboards**
- ? Developers see "Submit Work" button
- ? Sprint timer starts
- ? Team can now work on tasks

**System Notification**: "Sprint started successfully! Team members can now submit their work."

---

### Phase 2: Sprint Execution (Developers & Team)

#### Developer Dashboard View
When manager starts sprint, developers immediately see:

```
??????????????????????????????????????????????????????????????
? Active Sprints           ?
??????????????????????????????????????????????????????????????
?        ?
? Sprint 1    [Active ?]         3 days left     ?
? AB Payments USA          ?
? Jan 1, 2026 - Jan 23, 2026?
?      ?
? Progress: ???????????????? 40%             ?
? 20/50 pts           ?
?      ?
?  [Submit Work]   [View Report]           ?
??????????????????????????????????????????????????????????????
```

#### Developers Can:
1. **See Active Sprint Banner** (Dashboard & Project Page)
2. **Submit Sprint Work** (Multiple entry points):
   - Dashboard ? Active Sprint ? "Submit Work" button
   - Project Page ? Active Sprint Banner ? "Submit Work"  
   - Project Page ? Sprints Tab ? "Submit Work"

3. **Update Task Status** throughout sprint:
   - Move tasks through workflow (To Do ? In Progress ? Done)
   - Log time worked
   - Add comments
   - Update remaining hours

---

### Phase 3: Sprint Submission (Developers)

#### Submission Form Sections:

**1. Work Summary** (Required):
```
Story Points Planned: 15
Story Points Completed: 12
Hours Worked: 40
```

**2. User Stories Completed**:
```
Story ID: PROJ-123
Title: User Authentication
Story Points: 5
Status: Completed
```

**3. Achievements**:
```
- Implemented JWT authentication
- Created user profile page
- Fixed 3 critical bugs
```

**4. Learnings**:
```
- Learned React hooks in depth
- Improved understanding of API design
- Better at estimating story points
```

**5. Next Sprint Goals**:
```
- Complete payment integration
- Start working on admin dashboard
- Help with code reviews
```

#### Submission Options:
- **Save Draft**: Save progress, can edit later
- **Submit**: Final submission, cannot edit after

**Result**: Submission visible to manager in sprint report

---

### Phase 4: Sprint Monitoring (Manager)

#### Manager View During Active Sprint

**Project Dashboard Shows**:
```
??????????????????????????????????????????????????????????????
? Sprint 1     [Active ?] ?
? Jan 1 - Jan 23          60% Complete    [Complete] [?]    ?
?      ?
? Team Submissions:       ?
? ? John Doe - Submitted (12/15 pts)   ?
? ? Jane Smith - Submitted (10/10 pts)       ?
? ? Bob Wilson - Draft (8/12 pts)       ?
?     ?
?              [View Detailed Report]   ?
??????????????????????????????????????????????????????????????
```

#### Manager Can:
1. **View Sprint Reports** (Real-time):
   - Click "View Report" button
   - See live charts and metrics
   - Monitor team progress
   - Identify impediments

2. **Track Individual Performance**:
   - Story points completed vs planned
   - Hours worked per member
   - Task completion rate
   - Submission status

3. **Take Actions**:
   - Help with impediments
 - Rebalance workload
   - Update sprint timeline if needed
   - Communicate with team

---

### Phase 5: Sprint Reports (Manager)

#### Sprint Report Features

When manager clicks "View Report", they see:

**1. Overview Dashboard**:
```
????????????????????????????????????????????????????????????
?  Completion Rate    Total Hours    User Stories Impediments ?
?       78% 320h            45 3          ?
?    39/50 pts      12 members        12 features    1 open     ?
????????????????????????????????????????????????????????????
```

**2. Visual Charts**:

- **Team Performance Bar Chart**:
  - Shows planned vs completed points per member
  - Easy to spot over/under performers

- **Sprint Completion Pie Chart**:
  - Visual representation of completed vs remaining work
  - Color-coded (green for done, gray for remaining)

- **Hours Worked Area Chart**:
  - Time investment per team member
  - Identify work distribution patterns

- **Story Status Distribution**:
  - How many stories in each status
  - Completed, In Progress, Carry Forward

- **Team Radar Chart**:
  - Multi-dimensional team metrics
  - Completion rate, productivity, engagement

- **Impediments by Category**:
  - Technical, Process, External, etc.
  - Helps identify recurring issues

**3. Detailed Tables**:

- **Team Performance Table**:
```
Member    Planned  Completed  Hours  Stories  Status
John Doe    15     12       40      5      80% ?
Jane Smith  10    10       38 4     100% ?
Bob Wilson  12    8 32      3      67% ??
```

- **User Stories List**:
  - All stories submitted
  - Status, points, who worked on it

- **Features Delivered**:
  - What was delivered
  - Module, status, delivered by

- **Impediments Log**:
  - What blocked the team
  - Category, impact, resolution

- **Team Appreciations**:
  - Peer recognition
  - Builds team morale

**4. Export Options**:
- Export to PDF (coming soon)
- Share with stakeholders
- Archive for records

---

### Phase 6: Sprint Completion (Manager)

#### Step 1: Review Sprint
**Manager Actions**:
1. Go to Sprint Report
2. Review all submissions
3. Check completion percentage
4. Identify carry-forward items

#### Step 2: Complete Sprint
**Manager Actions**:
1. Go to Sprints tab
2. Find active sprint
3. Click "Complete" button
4. Confirm completion

**What Happens**:
- ? Sprint status changes to **Completed**
- ? Velocity calculated (story points completed)
- ? Incomplete tasks moved to backlog
- ? Sprint archived in history
- ? Team can start new sprint

**System Notification**: "Sprint completed! Velocity: 39 story points"

---

## How Developers See Manager Actions

### Scenario 1: Manager Creates Sprint
**Developer sees**: Nothing yet (sprint still in planning)

### Scenario 2: Manager Starts Sprint
**Developer sees**: 
- ?? Active sprint appears in dashboard
- ?? "Submit Work" button becomes visible
- ?? Sprint timer shows days remaining
- ?? Tasks are visible in sprint board

### Scenario 3: Manager Completes Sprint
**Developer sees**:
- ?? Sprint moves to "Completed" status
- ?? "Submit Work" button disappears
- ?? New sprint may start
- ?? Their submission is locked (read-only)

---

## Key Integration Points

### 1. Dashboard (Both Roles)
```
Manager Dashboard:
?? Active Sprints (with Edit options)
?? Team Performance Overview
?? Reports & Analytics
?? Sprint Management Buttons

Developer Dashboard:
?? Active Sprints (with Submit options)
?? My Tasks
?? My Submissions
?? Sprint Details
```

### 2. Project Page
```
Sprints Tab:
?? All Sprints List
?? Manager: Start/Complete/Edit/Delete
?? Developer: Submit Work (active only)
?? Status Badges (Planning/Active/Completed)
```

### 3. Sprint Report Page
```
Manager Only Access:
?? Real-time metrics
?? Visual charts & graphs
?? Team performance breakdown
?? Impediments tracking
?? Export functionality
?? Historical data
```

---

## Sprint Status Flow

```
[Planning] ??? Manager Clicks "Start" ???> [Active] ??? Manager Clicks "Complete" ???> [Completed]
     ?                 ?  ?
     ?         ?           ?
     ?         ?     ?
Manager can:       Developers can: Read-only for all
- Edit        - Submit work     - View in history
- Delete    - Update tasks          - See velocity
- Add tasks  - Log time   - See submissions
    Manager can:
           - View reports
          - Monitor progress
            - Complete sprint
```

---

## Real-Time Synchronization

When manager performs action, it immediately reflects for developers:

| Manager Action | Developer Sees | Timeframe |
|----------------|----------------|-----------|
| Start Sprint | Active sprint appears | Immediate (on page refresh) |
| Complete Sprint | Sprint moves to completed | Immediate (on page refresh) |
| Add Team Member | Access to project/sprint | Immediate |
| Update Sprint Details | Changes reflected | Immediate (on page refresh) |

**Note**: For real-time updates without refresh, implement WebSocket/SignalR (future enhancement)

---

## Best Practices

### For Managers:
1. ? Create sprint with clear goals
2. ? Assign tasks before starting
3. ? Start sprint at beginning of period
4. ? Monitor submissions throughout sprint
5. ? Review reports weekly
6. ? Complete sprint on time
7. ? Share velocity with team

### For Developers:
1. ? Check dashboard for active sprints
2. ? Update tasks daily
3. ? Submit work before sprint ends
4. ? Be honest about points completed
5. ? Document impediments
6. ? Give team appreciations
7. ? Review sprint goals

---

## Troubleshooting

### Issue: Developer doesn't see active sprint
**Solutions**:
1. Check if manager has started the sprint
2. Verify developer is in project team
3. Refresh the dashboard page
4. Check permission settings

### Issue: "Submit Work" button not visible
**Solutions**:
1. Verify sprint is in "Active" status (not Planning or Completed)
2. Check user role (only developers/team members see it)
3. Verify user is assigned to the project
4. Clear browser cache and refresh

### Issue: Manager can't complete sprint
**Solutions**:
1. Verify sprint is "Active" (can't complete Planning sprint)
2. Check permissions (must be Manager/Admin/Owner)
3. Ensure all submissions are reviewed
4. Check for system errors in console

---

## Feature Highlights

### Sprint Reports Include:
- ? Real-time metrics (completion %, hours, velocity)
- ? 7+ modern chart types (Bar, Pie, Area, Radar, etc.)
- ? Team performance breakdown
- ? Individual submission tracking
- ? Impediments analysis
- ? Feature delivery tracking
- ? Team appreciations showcase
- ? Export functionality (PDF)
- ? Mobile-responsive design
- ? Color-coded visualizations
- ? Drill-down capabilities
- ? Historical comparison (future)

---

## Summary

**Complete Sprint Workflow**:
1. Manager creates sprint (Planning status)
2. Manager adds tasks & assigns team
3. Manager starts sprint ?? **All developers see it immediately**
4. Developers submit work throughout sprint
5. Manager monitors via real-time reports
6. Manager completes sprint at end
7. Sprint data archived with velocity metrics

**Developer Experience**:
- Simple & clear active sprint visibility
- Easy "Submit Work" access from multiple places
- Real-time progress tracking
- Clear submission status

**Manager Experience**:
- Full sprint lifecycle management
- Rich visual analytics & reports
- Team performance insights
- Data-driven decision making
- Export & sharing capabilities

?? **Result**: Seamless sprint management with complete visibility for all stakeholders!
