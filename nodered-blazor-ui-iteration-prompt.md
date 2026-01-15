# NodeRed.NET UI Deep Iteration Prompt

## Context

You have already translated the core Node-RED functionality to NodeRed.NET. This prompt focuses exclusively on achieving **pixel-perfect, interaction-perfect UI fidelity** with the original Node-RED editor. The UI is the soul of Node-RED - every click, drag, hover, and visual detail matters.

You have access to the Node-RED repository at https://github.com/node-red/node-red. The editor client lives in `packages/node_modules/@node-red/editor-client/`.

---

## MISSION: EXACT UI REPLICATION

Your task is to audit and perfect the NodeRed.NET Blazor editor until it is **indistinguishable** from the original Node-RED editor. A user familiar with Node-RED should feel completely at home - every icon, every hover state, every drag behavior, every panel transition must match.

---

## CRITICAL UI CONSTRAINTS

### Rule 1: Visual Pixel Comparison

For every UI element, you must:

1. Screenshot or describe the original Node-RED element in detail
2. List every visual property (colors, sizes, margins, borders, shadows, icons)
3. Compare against your Blazor implementation
4. Fix ALL discrepancies, no matter how small

### Rule 2: Interaction Frame-by-Frame Analysis

For every interaction, you must:

1. Describe the exact sequence of events in Node-RED (mousedown, mousemove, mouseup, click, dblclick, contextmenu, wheel, keydown, etc.)
2. Document what visual feedback occurs at each step
3. Document cursor changes
4. Document any animations or transitions
5. Replicate EXACTLY in Blazor

### Rule 3: No "Close Enough"

These phrases are FORBIDDEN:

- "Similar to Node-RED..."
- "Roughly matches..."
- "Close enough to..."
- "Simplified version of..."
- "Works like the original but..."
- "Minor differences include..."

If it's not EXACT, it's WRONG. Fix it.

---

## UI AUDIT CHECKLIST

Go through each section systematically. For each item, provide:

```
## [Component Name]

### Source Files
- Editor HTML: [path]
- CSS: [path]  
- JavaScript: [path]

### Visual Audit
| Property | Node-RED Value | NodeRed.NET Value | Match? |
|----------|----------------|-------------------|--------|
| Width | | | |
| Height | | | |
| Background | | | |
| Border | | | |
| Border Radius | | | |
| Box Shadow | | | |
| Font Family | | | |
| Font Size | | | |
| Font Weight | | | |
| Color | | | |
| Padding | | | |
| Margin | | | |
| Icon | | | |
| Icon Size | | | |

### Interaction Audit
| Event | Node-RED Behavior | NodeRed.NET Behavior | Match? |
|-------|-------------------|----------------------|--------|
| hover | | | |
| mousedown | | | |
| mouseup | | | |
| click | | | |
| dblclick | | | |
| contextmenu | | | |
| drag | | | |
| keydown | | | |

### Fixes Required
- [ ] Fix 1
- [ ] Fix 2
```

---

## SECTION 1: WORKSPACE CANVAS

Analyze `packages/node_modules/@node-red/editor-client/src/js/ui/view.js` exhaustively.

### 1.1 Canvas Background

- Grid pattern (dots vs lines, spacing, color)
- Background color
- How grid scales with zoom

### 1.2 Pan Behavior

- Middle mouse button drag
- Spacebar + left mouse drag
- Two-finger trackpad pan
- Pan momentum/inertia (if any)
- Pan boundaries (infinite canvas or limits?)
- Cursor icon during pan

### 1.3 Zoom Behavior

- Mouse wheel zoom
- Trackpad pinch zoom
- Zoom center point (mouse position vs canvas center)
- Zoom min/max limits
- Zoom step increments
- Zoom animation/smoothness
- Zoom level display in UI

### 1.4 Selection

- Click to select single node
- Click on empty space to deselect
- Ctrl+click to add to selection
- Shift+click behavior
- Selection lasso (rubber band):
  - When does it activate? (click vs drag threshold)
  - Lasso visual style (color, border, fill opacity)
  - Which direction creates lasso vs pan?
  - Selection logic (intersect vs fully contained)
- Select all (Ctrl+A)
- Selection visual feedback on nodes

### 1.5 Canvas Context Menu

- Right-click on empty canvas
- All menu items and their actions
- Keyboard shortcuts shown in menu
- Submenu behavior
- Menu positioning logic

---

## SECTION 2: NODE RENDERING

Analyze `packages/node_modules/@node-red/editor-client/src/js/ui/view.js` (node rendering parts) and node HTML templates.

### 2.1 Node Dimensions

- Default node width (how is it calculated?)
- Node height
- Port size and position
- Label positioning
- Icon positioning and size
- Status dot position

### 2.2 Node Visual States

Document exact colors/styles for:

- Default/idle state
- Hover state
- Selected state
- Selected + hover state
- Disabled state
- Node with errors
- Unknown node type
- Subflow instance node

### 2.3 Node Shape Variations

- Standard rectangle
- Input-only nodes (no left port)
- Output-only nodes (no right port)
- Nodes with status
- Config nodes appearance
- Link nodes appearance
- Comment node appearance
- Junction node appearance

### 2.4 Node Ports

- Input port visual (left side)
- Output port visual (right side)
- Port hover state
- Port colors (do they vary by type?)
- Multiple output ports spacing
- Port labels (if any)

### 2.5 Node Icons

- Where do icons come from? (Font Awesome? Custom?)
- Icon catalog - list ALL icons used
- Icon colors
- Icon sizing
- Custom node icons loading

### 2.6 Node Labels

- Primary label (node name or type)
- Label truncation with ellipsis
- Label font, size, color
- Sublabel/status text below

### 2.7 Node Interactions

- Hover: what changes?
- Click: selection visual
- Double-click: opens editor
- Right-click: context menu
- Drag threshold before move starts
- Drag visual feedback
- Multi-select drag
- Drag cursor icon
- Drop zone feedback
- Node alignment guides while dragging
- Snap to grid behavior

### 2.8 Node Context Menu

- All menu items
- Conditional menu items (copy, cut, delete, edit, enable, disable, etc.)
- Menu for multiple selected nodes
- Keyboard shortcut hints

---

## SECTION 3: WIRES/LINKS

Analyze wire rendering in `view.js`.

### 3.1 Wire Geometry

- Bézier curve calculation (exact control point formula)
- Wire thickness
- Wire color (default, selected, hover)
- Wire color by link type (if any)

### 3.2 Wire Drawing

- Drag from port to start wire
- Wire preview while dragging
- Wire color during drag
- Valid vs invalid drop target feedback
- Cancel wire drawing (right-click, Escape)

### 3.3 Wire Selection

- Click on wire to select
- Wire hit detection (how close to wire?)
- Selected wire visual
- Delete selected wire

### 3.4 Wire Interactions

- Hover on wire
- Click on wire
- Double-click on wire (quick add node?)
- Right-click on wire (context menu)
- Ctrl+click on wire to insert node
- Moving a wire (detach and reattach)

### 3.5 Link Nodes (Virtual Wires)

- Link in/out appearance
- Link call appearance
- How virtual connections are visualized
- Link hover behavior

---

## SECTION 4: PALETTE (LEFT PANEL)

Analyze `packages/node_modules/@node-red/editor-client/src/js/ui/palette.js`.

### 4.1 Palette Layout

- Panel width (fixed or resizable?)
- Header appearance
- Search box styling
- Category headers

### 4.2 Palette Nodes

- Node preview appearance in palette
- Node icon in palette
- Node label in palette
- Disabled node appearance
- Filtered (no match) appearance

### 4.3 Categories

- Category header style
- Collapse/expand behavior
- Collapse animation
- Category icons
- Default expanded state
- Remember expanded state

### 4.4 Search/Filter

- Search input styling
- Filter logic (name, type, description?)
- Highlight matching text
- Clear search button
- No results message

### 4.5 Palette Interactions

- Drag node from palette
- Drag preview appearance
- Drop on canvas
- Invalid drop feedback
- Double-click to add node at center
- Tooltip on hover

### 4.6 Palette Scrolling

- Scrollbar styling
- Scroll behavior

---

## SECTION 5: SIDEBAR (RIGHT PANEL)

Analyze `packages/node_modules/@node-red/editor-client/src/js/ui/sidebar.js`.

### 5.1 Sidebar Tabs

- Info tab
- Debug tab
- Config nodes tab
- Context data tab
- Help tab (if present)
- Tab header styling
- Active tab indicator
- Tab icons

### 5.2 Sidebar Resize

- Drag to resize
- Resize handle appearance
- Minimum/maximum width
- Collapse sidebar
- Remember width

### 5.3 Info Tab

- Node info display
- Markdown rendering
- Tips section
- Layout and typography

### 5.4 Debug Tab

- Debug message appearance
- Message expansion/collapse
- Message path display
- Object/array rendering
- Timestamp format
- Source node link
- Filter by node
- Clear button
- Auto-scroll behavior
- Message limit

### 5.5 Config Nodes Tab

- Config node list
- Grouping by type
- Usage count
- Add config node
- Edit config node
- Unused config node indicator

---

## SECTION 6: TRAY (NODE EDIT PANEL)

Analyze `packages/node_modules/@node-red/editor-client/src/js/ui/tray.js` and node HTML files.

### 6.1 Tray Animation

- Slide in from right
- Animation duration and easing
- Backdrop/overlay appearance
- Multiple trays stacking

### 6.2 Tray Header

- Title
- Close button
- Done/Update/Delete buttons
- Button styling

### 6.3 Tray Content

- Form field styling
- Input fields
- Select dropdowns
- Checkboxes
- TypedInput controls (the special multi-type inputs)
- Code editor (Monaco/Ace?) for function nodes
- Color pickers
- JSON editors

### 6.4 TypedInput Widget

This is complex - analyze `packages/node_modules/@node-red/editor-client/src/js/ui/common/typedInput.js`:

- Type selector button
- Type menu
- All type options (msg, flow, global, str, num, bool, json, bin, date, env, etc.)
- Type-specific input rendering
- Validation per type

### 6.5 Tray Interactions

- Close on Escape
- Close on click outside (or not?)
- Form validation
- Unsaved changes warning
- Tab key navigation

---

## SECTION 7: TAB BAR (FLOW TABS)

Analyze `packages/node_modules/@node-red/editor-client/src/js/ui/tab.js` and workspace tabs.

### 7.1 Tab Appearance

- Active tab style
- Inactive tab style
- Tab height
- Tab icon
- Tab label truncation
- Tab close button (if any)
- Tab dirty indicator (unsaved changes)

### 7.2 Tab Interactions

- Click to switch
- Double-click to rename
- Drag to reorder
- Right-click menu
- Add new tab button
- Scroll tabs when many

### 7.3 Tab Context Menu

- Rename
- Delete
- Enable/disable
- Show info
- Add subflow input/output

---

## SECTION 8: HEADER/TOOLBAR

### 8.1 Deploy Button

- Button appearance
- Dropdown with deploy modes
- Modified indicator
- Deploy animation/feedback
- Disabled state

### 8.2 Menu (Hamburger)

- All menu items
- Submenus
- Keyboard shortcuts display
- Icons

### 8.3 User Menu

- User avatar/icon
- Login/logout

---

## SECTION 9: DIALOGS/MODALS

### 9.1 Import Dialog

- Full appearance
- Tabs (clipboard, file, examples)
- Code input area
- Import options
- Buttons

### 9.2 Export Dialog

- Full appearance
- Options (selected, flow, all)
- Format options
- Copy/download buttons

### 9.3 Search Dialog (Ctrl+F)

- Search input
- Results list
- Result item appearance
- Navigate to result

### 9.4 Manage Palette Dialog

- Nodes tab
- Install tab
- Search
- Enable/disable nodes
- Install progress

### 9.5 Confirm Dialogs

- Delete confirmation
- Unsaved changes
- Deploy warnings

---

## SECTION 10: KEYBOARD SHORTCUTS

List and verify EVERY keyboard shortcut:

```
| Shortcut | Action | Implemented? | Works Correctly? |
|----------|--------|--------------|------------------|
| Ctrl+A | Select all | | |
| Ctrl+C | Copy | | |
| Ctrl+V | Paste | | |
| Ctrl+X | Cut | | |
| Ctrl+Z | Undo | | |
| Ctrl+Y | Redo | | |
| Ctrl+Shift+Z | Redo | | |
| Delete | Delete selected | | |
| Backspace | Delete selected | | |
| Ctrl+D | Deploy | | |
| Ctrl+Shift+D | Deploy (modified) | | |
| Ctrl+E | Export | | |
| Ctrl+I | Import | | |
| Ctrl+F | Search | | |
| Ctrl+G | Group | | |
| Ctrl+Shift+G | Ungroup | | |
| Escape | Cancel/close | | |
| Enter | Confirm | | |
| Tab | Next field | | |
| Shift+Tab | Previous field | | |
| Arrow keys | Move selection | | |
| Shift+Arrow | Nudge nodes | | |
... [continue for ALL shortcuts]
```

---

## SECTION 11: ANIMATIONS & TRANSITIONS

Document every animation:

- Tray slide in/out
- Panel collapse/expand
- Node status pulse
- Deploy progress
- Notification slide in
- Tooltip fade
- Dropdown menus
- Context menus
- Loading spinners

For each:
- Duration (ms)
- Easing function
- CSS or JavaScript animation?

---

## SECTION 12: ICONS INVENTORY

Create a complete inventory of every icon used:

```
| Icon | Where Used | Source (FA class / SVG path / custom) | NodeRed.NET Implementation |
|------|------------|---------------------------------------|----------------------------|
| | | | |
```

Analyze:
- `packages/node_modules/@node-red/editor-client/src/images/`
- Font Awesome usage in CSS/HTML
- Inline SVGs
- Node-specific icons

---

## SECTION 13: CSS/THEMING

Analyze all CSS files in `packages/node_modules/@node-red/editor-client/src/sass/`.

### 13.1 Color Variables

Extract ALL color definitions:
- Primary colors
- Secondary colors
- Background colors
- Border colors
- Text colors
- Status colors (red, yellow, green)
- Error colors
- Success colors

### 13.2 Typography

- Font families used
- Font sizes (all variations)
- Font weights
- Line heights
- Letter spacing

### 13.3 Spacing System

- Margin values used
- Padding values used
- Grid/gap values

### 13.4 Dark Theme

If Node-RED has dark theme support:
- How is it toggled?
- All color overrides

---

## SECTION 14: RESPONSIVE BEHAVIOR

- Minimum window size
- Panel resize constraints
- What happens on small screens?
- Mobile/touch adaptations (if any)

---

## SECTION 15: CURSOR INVENTORY

Document every cursor state:

```
| Context | Cursor | CSS Value |
|---------|--------|-----------|
| Default canvas | | |
| Hovering node | | |
| Dragging node | | |
| Drawing wire | | |
| Panning canvas | | |
| Resizing panel | | |
| Over port | | |
| Over link/wire | | |
| Disabled element | | |
| Text input | | |
| Loading | | |
```

---

## SECTION 16: SUBFLOWS

Analyze `packages/node_modules/@node-red/editor-client/src/js/ui/subflow.js` and related files.

### 16.1 Subflow Definition Node

- Appearance in the canvas when editing a subflow
- Subflow input port node (left side)
- Subflow output port node (right side)
- Subflow status node
- Visual distinction from regular nodes
- Color/styling

### 16.2 Subflow Instance Node

- How a subflow instance appears when used in a flow
- Color inherited from subflow definition
- Icon display
- Label display
- Port count matching subflow definition
- Visual indicator that it's a subflow (not regular node)

### 16.3 Subflow Palette Entry

- How subflows appear in palette
- Category (subflows category)
- Icon
- Drag behavior

### 16.4 Subflow Tab/Workspace

- Tab appearance for subflow editing
- Tab icon (different from flow tabs?)
- Visual distinction in tab bar
- Subflow properties in tab

### 16.5 Subflow Editor Tray

- Subflow properties editing
- Name field
- Category selection
- Color picker
- Icon picker
- Description/info (markdown)
- Input labels configuration
- Output labels configuration
- Environment variables definition UI
  - Add variable button
  - Variable name input
  - Variable type selector
  - Variable default value
  - Variable UI options (hidden, credential, etc.)
  - Reorder variables
  - Delete variable

### 16.6 Subflow Input/Output Configuration

- Adding inputs to subflow
- Adding outputs to subflow
- Maximum input/output limits
- Visual representation of ports being added
- Wiring to subflow input/output nodes inside subflow

### 16.7 Subflow Instance Properties Tray

- Editing a subflow instance
- Environment variable overrides
- Instance name
- Display of inherited vs overridden values

### 16.8 Subflow Creation

- "Selection to Subflow" action
- Dialog/wizard for creating subflow from selection
- Automatic port detection
- Naming the new subflow

### 16.9 Subflow Context Menu

- Right-click on subflow instance
- "Edit subflow" option
- "Go to subflow definition" option
- Other subflow-specific actions

### 16.10 Subflow Status

- Status node inside subflow
- How status propagates to instance
- Visual feedback

### 16.11 Subflow Template Expansion

- "Convert to nodes" action
- Inlining a subflow instance
- Visual representation during conversion

---

## SECTION 17: GROUPS

Analyze `packages/node_modules/@node-red/editor-client/src/js/ui/group.js` and related files.

### 17.1 Group Visual Appearance

- Group rectangle/boundary
- Border style (color, thickness, dash pattern?)
- Background fill (color, opacity)
- Corner radius
- Group name/label position
- Group name styling (font, size, color)

### 17.2 Group Colors

- Default group color
- Color picker in group editor
- Predefined color palette
- Custom color input
- How color applies (border, fill, both?)

### 17.3 Group Selection

- Click on group border to select group
- Click inside group on empty space
- Click on node inside group (select node or group?)
- Ctrl+click behavior with groups
- Select all nodes in group
- Selection visual (group selected vs nodes selected)

### 17.4 Group Resize

- Resize handles appearance
- Resize handle positions (corners, edges?)
- Resize cursor
- Minimum group size
- Resize snapping
- Auto-resize to fit contents

### 17.5 Group Drag/Move

- Dragging the group
- Does dragging group move contained nodes?
- Dragging nodes within group
- Dragging nodes out of group
- Dragging nodes into group
- Visual feedback during drag (highlight drop zone?)

### 17.6 Group Creation

- Keyboard shortcut (Ctrl+G)
- Menu action
- Creating group from selected nodes
- Creating empty group
- Initial group size calculation

### 17.7 Group Editor Tray

- Group name field
- Group color picker
- Group class/style (if any)
- Environment variables for group

### 17.8 Group Nesting

- Can groups contain other groups?
- Visual representation of nested groups
- Z-order of nested groups
- Selection behavior with nested groups

### 17.9 Group Context Menu

- Right-click on group
- "Ungroup" option
- "Edit group" option
- "Select all in group" option
- Other group-specific actions

### 17.10 Group and Wires

- Wires entering/exiting group
- Wire routing around group boundary
- Wire visual when crossing group border

### 17.11 Group Interactions

| Event | Behavior |
|-------|----------|
| Click on border | |
| Click inside (empty) | |
| Click inside (on node) | |
| Double-click on group | |
| Right-click on group | |
| Drag group | |
| Drag node into group | |
| Drag node out of group | |
| Resize handles | |
| Ctrl+G on selection | |
| Delete group (keeps nodes?) | |
| Delete group (deletes nodes?) | |
| Copy group | |
| Paste group | |
| Cut group | |

### 17.12 Group and Clipboard

- Copy group with contents
- Paste group behavior
- JSON representation of group in flow

### 17.13 Group in Subflows

- Can groups exist inside subflows?
- Any restrictions?

### 17.14 Group Z-Order

- Group rendering order (behind nodes?)
- Multiple overlapping groups
- Bring to front / send to back

---

## SECTION 18: SUBFLOW + GROUP COMBINED SCENARIOS

### 18.1 Groups Inside Subflows

- Creating groups in subflow editing mode
- Visual behavior
- Any limitations

### 18.2 Subflow Instances Inside Groups

- Placing subflow instances in groups
- Visual representation
- Drag behavior

### 18.3 Selection Combinations

- Selecting mix of groups, nodes, subflow instances
- Copy/paste mixed selection
- Delete mixed selection
- Align/distribute mixed selection

---

## OUTPUT FORMAT

For each section, provide:

1. **Detailed analysis** of the original Node-RED implementation
2. **Current NodeRed.NET state** (what exists now)
3. **Discrepancy list** (every difference found)
4. **Fix implementation** (exact code changes needed)
5. **Verification** (how to confirm the fix is correct)

Wait for "APPROVED" after each section before proceeding to the next.

---

## TESTING METHODOLOGY

For UI verification:

1. Open Node-RED in one browser window
2. Open NodeRed.NET in another browser window
3. Place side by side
4. Perform identical actions in both
5. Compare visually frame-by-frame
6. Document any difference, no matter how subtle

---

## BEGIN

Start with **Section 1: Workspace Canvas**. Analyze the original exhaustively, compare to current implementation, and provide fixes.

Do not proceed to Section 2 until Section 1 receives "APPROVED".
