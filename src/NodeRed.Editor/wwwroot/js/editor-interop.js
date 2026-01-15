// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/view.js
//         packages/node_modules/@node-red/editor-client/src/js/ui/keyboard.js
//         packages/node_modules/@node-red/editor-client/src/js/ui/touch/radialMenu.js
// ============================================================
// JavaScript interop for Blazor editor interactions.
// Handles canvas interactions, keyboard shortcuts, and drag/drop.
// ============================================================

window.nodeRedEditor = {
    // ============================================================
    // CANVAS INTERACTIONS
    // Translated from: packages/node_modules/@node-red/editor-client/src/js/ui/view.js
    // Lines: 200-400 (mouse event handling)
    // ============================================================
    
    // State for canvas interactions
    state: {
        dragging: false,
        dragNode: null,
        dragStartX: 0,
        dragStartY: 0,
        connecting: false,
        connectionSource: null,
        connectionPort: null,
        lasso: false,
        lassoStart: { x: 0, y: 0 },
        panning: false,
        panStartX: 0,
        panStartY: 0,
        scale: 1.0,
        offsetX: 0,
        offsetY: 0
    },

    // Initialize canvas event listeners
    initCanvas: function(canvasElement, dotNetRef) {
        if (!canvasElement) return;
        
        const self = this;
        
        // ============================================================
        // SOURCE: view.js lines 250-280 - mousedown handling
        // ============================================================
        canvasElement.addEventListener('mousedown', function(e) {
            const rect = canvasElement.getBoundingClientRect();
            const x = (e.clientX - rect.left) / self.state.scale - self.state.offsetX;
            const y = (e.clientY - rect.top) / self.state.scale - self.state.offsetY;
            
            if (e.button === 0) { // Left click
                if (e.target.classList.contains('red-ui-workspace-port-output')) {
                    // Start wire drawing
                    self.state.connecting = true;
                    self.state.connectionSource = e.target.closest('.red-ui-workspace-node');
                    self.state.connectionPort = e.target;
                    dotNetRef.invokeMethodAsync('OnWireDrawStart', 
                        self.state.connectionSource?.dataset.nodeId || '', 
                        parseInt(e.target.dataset.portIndex || '0'),
                        x, y);
                } else if (e.target.closest('.red-ui-workspace-node')) {
                    // Start node drag
                    const nodeEl = e.target.closest('.red-ui-workspace-node');
                    self.state.dragging = true;
                    self.state.dragNode = nodeEl;
                    self.state.dragStartX = x;
                    self.state.dragStartY = y;
                    dotNetRef.invokeMethodAsync('OnNodeDragStart', 
                        nodeEl.dataset.nodeId || '', x, y);
                } else {
                    // Start lasso selection
                    self.state.lasso = true;
                    self.state.lassoStart = { x: x, y: y };
                    dotNetRef.invokeMethodAsync('OnLassoStart', x, y);
                }
            } else if (e.button === 1) { // Middle click - pan
                self.state.panning = true;
                self.state.panStartX = e.clientX;
                self.state.panStartY = e.clientY;
                canvasElement.style.cursor = 'move';
            }
        });

        // ============================================================
        // SOURCE: view.js lines 300-350 - mousemove handling
        // ============================================================
        canvasElement.addEventListener('mousemove', function(e) {
            const rect = canvasElement.getBoundingClientRect();
            const x = (e.clientX - rect.left) / self.state.scale - self.state.offsetX;
            const y = (e.clientY - rect.top) / self.state.scale - self.state.offsetY;
            
            if (self.state.dragging && self.state.dragNode) {
                const dx = x - self.state.dragStartX;
                const dy = y - self.state.dragStartY;
                dotNetRef.invokeMethodAsync('OnNodeDrag', 
                    self.state.dragNode.dataset.nodeId || '', dx, dy);
            } else if (self.state.connecting) {
                dotNetRef.invokeMethodAsync('OnWireDraw', x, y);
            } else if (self.state.lasso) {
                dotNetRef.invokeMethodAsync('OnLassoDraw', 
                    self.state.lassoStart.x, self.state.lassoStart.y, x, y);
            } else if (self.state.panning) {
                const dx = e.clientX - self.state.panStartX;
                const dy = e.clientY - self.state.panStartY;
                self.state.offsetX += dx / self.state.scale;
                self.state.offsetY += dy / self.state.scale;
                self.state.panStartX = e.clientX;
                self.state.panStartY = e.clientY;
                dotNetRef.invokeMethodAsync('OnCanvasPan', 
                    self.state.offsetX, self.state.offsetY);
            }
        });

        // ============================================================
        // SOURCE: view.js lines 360-400 - mouseup handling
        // ============================================================
        canvasElement.addEventListener('mouseup', function(e) {
            const rect = canvasElement.getBoundingClientRect();
            const x = (e.clientX - rect.left) / self.state.scale - self.state.offsetX;
            const y = (e.clientY - rect.top) / self.state.scale - self.state.offsetY;
            
            if (self.state.dragging) {
                dotNetRef.invokeMethodAsync('OnNodeDragEnd', 
                    self.state.dragNode?.dataset.nodeId || '', x, y);
                self.state.dragging = false;
                self.state.dragNode = null;
            } else if (self.state.connecting) {
                // Check if over an input port
                const targetPort = document.elementFromPoint(e.clientX, e.clientY);
                if (targetPort?.classList.contains('red-ui-workspace-port-input')) {
                    const targetNode = targetPort.closest('.red-ui-workspace-node');
                    dotNetRef.invokeMethodAsync('OnWireDrawEnd', 
                        targetNode?.dataset.nodeId || '',
                        parseInt(targetPort.dataset.portIndex || '0'),
                        true);
                } else {
                    dotNetRef.invokeMethodAsync('OnWireDrawEnd', '', 0, false);
                }
                self.state.connecting = false;
                self.state.connectionSource = null;
            } else if (self.state.lasso) {
                dotNetRef.invokeMethodAsync('OnLassoEnd', 
                    self.state.lassoStart.x, self.state.lassoStart.y, x, y);
                self.state.lasso = false;
            } else if (self.state.panning) {
                self.state.panning = false;
                canvasElement.style.cursor = 'default';
            }
        });

        // ============================================================
        // SOURCE: view.js lines 420-450 - wheel handling (zoom)
        // ============================================================
        canvasElement.addEventListener('wheel', function(e) {
            e.preventDefault();
            const rect = canvasElement.getBoundingClientRect();
            const mouseX = e.clientX - rect.left;
            const mouseY = e.clientY - rect.top;
            
            const delta = e.deltaY > 0 ? 0.9 : 1.1;
            const newScale = Math.max(0.25, Math.min(2.0, self.state.scale * delta));
            
            // Zoom towards mouse position
            const scaleChange = newScale / self.state.scale;
            self.state.offsetX = mouseX - (mouseX - self.state.offsetX) * scaleChange;
            self.state.offsetY = mouseY - (mouseY - self.state.offsetY) * scaleChange;
            self.state.scale = newScale;
            
            dotNetRef.invokeMethodAsync('OnCanvasZoom', 
                self.state.scale, self.state.offsetX, self.state.offsetY);
        }, { passive: false });

        // ============================================================
        // SOURCE: view.js lines 460-480 - context menu
        // ============================================================
        canvasElement.addEventListener('contextmenu', function(e) {
            e.preventDefault();
            const rect = canvasElement.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            
            const targetNode = e.target.closest('.red-ui-workspace-node');
            dotNetRef.invokeMethodAsync('OnContextMenu', 
                targetNode?.dataset.nodeId || '', x, y);
        });

        // Double-click handling
        canvasElement.addEventListener('dblclick', function(e) {
            const targetNode = e.target.closest('.red-ui-workspace-node');
            if (targetNode) {
                dotNetRef.invokeMethodAsync('OnNodeDoubleClick', 
                    targetNode.dataset.nodeId || '');
            }
        });
    },

    // Set canvas transform
    setCanvasTransform: function(canvasElement, scale, offsetX, offsetY) {
        this.state.scale = scale;
        this.state.offsetX = offsetX;
        this.state.offsetY = offsetY;
        
        const svg = canvasElement?.querySelector('svg');
        if (svg) {
            svg.style.transform = `scale(${scale}) translate(${offsetX}px, ${offsetY}px)`;
            svg.style.transformOrigin = '0 0';
        }
    },

    // Reset canvas view
    resetView: function(canvasElement) {
        this.state.scale = 1.0;
        this.state.offsetX = 0;
        this.state.offsetY = 0;
        this.setCanvasTransform(canvasElement, 1.0, 0, 0);
    },

    // ============================================================
    // KEYBOARD SHORTCUTS
    // Translated from: packages/node_modules/@node-red/editor-client/src/js/ui/keyboard.js
    // Lines: 50-200 (shortcut definitions)
    // ============================================================
    
    keyboardShortcuts: {},
    dotNetRef: null,

    initKeyboard: function(dotNetRef) {
        this.dotNetRef = dotNetRef;
        const self = this;

        // ============================================================
        // SOURCE: keyboard.js lines 100-180 - key mappings
        // ============================================================
        document.addEventListener('keydown', function(e) {
            // Don't trigger shortcuts when typing in inputs
            if (e.target.tagName === 'INPUT' || 
                e.target.tagName === 'TEXTAREA' || 
                e.target.isContentEditable) {
                // But still handle Escape
                if (e.key === 'Escape') {
                    dotNetRef.invokeMethodAsync('OnKeyboardShortcut', 'escape');
                }
                return;
            }

            const key = e.key.toLowerCase();
            const ctrl = e.ctrlKey || e.metaKey;
            const shift = e.shiftKey;
            const alt = e.altKey;

            let shortcut = '';
            if (ctrl) shortcut += 'ctrl+';
            if (shift) shortcut += 'shift+';
            if (alt) shortcut += 'alt+';
            shortcut += key;

            // ============================================================
            // Node-RED keyboard shortcuts (from keyboard.js)
            // ============================================================
            const shortcuts = {
                'ctrl+a': 'selectAll',
                'ctrl+c': 'copy',
                'ctrl+x': 'cut',
                'ctrl+v': 'paste',
                'ctrl+z': 'undo',
                'ctrl+shift+z': 'redo',
                'ctrl+y': 'redo',
                'delete': 'deleteSelected',
                'backspace': 'deleteSelected',
                'ctrl+d': 'deploy',
                'ctrl+shift+d': 'deployModified',
                'ctrl+i': 'import',
                'ctrl+e': 'export',
                'ctrl+f': 'search',
                'ctrl+shift+p': 'managePalette',
                'escape': 'escape',
                'tab': 'nextTab',
                'shift+tab': 'prevTab',
                'ctrl+shift+j': 'showDebug',
                'ctrl+g i': 'showInfo',
                'ctrl+g c': 'showConfig',
                '+': 'zoomIn',
                '=': 'zoomIn',
                '-': 'zoomOut',
                'ctrl+0': 'zoomReset',
                'arrowup': 'moveUp',
                'arrowdown': 'moveDown',
                'arrowleft': 'moveLeft',
                'arrowright': 'moveRight',
                'shift+arrowup': 'moveUpFine',
                'shift+arrowdown': 'moveDownFine',
                'shift+arrowleft': 'moveLeftFine',
                'shift+arrowright': 'moveRightFine'
            };

            if (shortcuts[shortcut]) {
                e.preventDefault();
                dotNetRef.invokeMethodAsync('OnKeyboardShortcut', shortcuts[shortcut]);
            }
        });

        document.addEventListener('keyup', function(e) {
            if (e.key === 'Shift' || e.key === 'Control' || e.key === 'Meta') {
                dotNetRef.invokeMethodAsync('OnModifierKeyUp', e.key.toLowerCase());
            }
        });
    },

    // ============================================================
    // DRAG AND DROP (Palette to Canvas)
    // Translated from: packages/node_modules/@node-red/editor-client/src/js/ui/palette.js
    // Lines: 150-200 (drag handling)
    // ============================================================
    
    initPaletteDrag: function(paletteElement, canvasElement, dotNetRef) {
        if (!paletteElement) return;

        const self = this;
        let dragData = null;
        let dragPreview = null;

        paletteElement.addEventListener('dragstart', function(e) {
            const nodeEl = e.target.closest('.red-ui-palette-node');
            if (!nodeEl) return;

            dragData = {
                type: nodeEl.dataset.nodeType || '',
                label: nodeEl.dataset.nodeLabel || '',
                color: nodeEl.dataset.nodeColor || '#ddd',
                inputs: parseInt(nodeEl.dataset.nodeInputs || '1'),
                outputs: parseInt(nodeEl.dataset.nodeOutputs || '1')
            };

            // Create drag preview
            dragPreview = document.createElement('div');
            dragPreview.className = 'red-ui-palette-node-drag-preview';
            dragPreview.style.cssText = `
                position: fixed;
                pointer-events: none;
                background: ${dragData.color};
                border: 1px solid #999;
                border-radius: 5px;
                padding: 5px 10px;
                font-size: 12px;
                z-index: 10000;
                opacity: 0.8;
            `;
            dragPreview.textContent = dragData.type;
            document.body.appendChild(dragPreview);

            // Set drag image (invisible)
            const img = new Image();
            img.src = 'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7';
            e.dataTransfer.setDragImage(img, 0, 0);
            e.dataTransfer.effectAllowed = 'copy';
            e.dataTransfer.setData('text/plain', JSON.stringify(dragData));

            dotNetRef.invokeMethodAsync('OnPaletteDragStart', dragData.type);
        });

        document.addEventListener('drag', function(e) {
            if (dragPreview && e.clientX && e.clientY) {
                dragPreview.style.left = (e.clientX + 10) + 'px';
                dragPreview.style.top = (e.clientY + 10) + 'px';
            }
        });

        document.addEventListener('dragend', function(e) {
            if (dragPreview) {
                dragPreview.remove();
                dragPreview = null;
            }
            dragData = null;
        });

        if (canvasElement) {
            canvasElement.addEventListener('dragover', function(e) {
                e.preventDefault();
                e.dataTransfer.dropEffect = 'copy';
            });

            canvasElement.addEventListener('drop', function(e) {
                e.preventDefault();
                if (!dragData) return;

                const rect = canvasElement.getBoundingClientRect();
                const x = (e.clientX - rect.left) / self.state.scale - self.state.offsetX;
                const y = (e.clientY - rect.top) / self.state.scale - self.state.offsetY;

                // Snap to grid (20px)
                const snappedX = Math.round(x / 20) * 20;
                const snappedY = Math.round(y / 20) * 20;

                dotNetRef.invokeMethodAsync('OnNodeDropped', 
                    dragData.type,
                    dragData.label,
                    dragData.color,
                    dragData.inputs,
                    dragData.outputs,
                    snappedX,
                    snappedY);
            });
        }
    },

    // ============================================================
    // CLIPBOARD OPERATIONS
    // Translated from: packages/node_modules/@node-red/editor-client/src/js/ui/clipboard.js
    // ============================================================
    
    clipboard: null,

    copyToClipboard: function(data) {
        this.clipboard = data;
        // Also copy to system clipboard as JSON
        navigator.clipboard.writeText(JSON.stringify(data)).catch(() => {});
    },

    pasteFromClipboard: function() {
        return this.clipboard;
    },

    // ============================================================
    // NOTIFICATIONS
    // Translated from: packages/node_modules/@node-red/editor-client/src/js/ui/notifications.js
    // ============================================================
    
    notify: function(message, type, timeout) {
        type = type || 'info';
        timeout = timeout || 3000;

        const container = document.querySelector('.red-ui-notifications') || 
            (() => {
                const el = document.createElement('div');
                el.className = 'red-ui-notifications';
                document.body.appendChild(el);
                return el;
            })();

        const notification = document.createElement('div');
        notification.className = `red-ui-notification red-ui-notification-${type}`;
        notification.innerHTML = `
            <span class="red-ui-notification-message">${message}</span>
            <button class="red-ui-notification-close">&times;</button>
        `;

        notification.querySelector('.red-ui-notification-close').addEventListener('click', () => {
            notification.remove();
        });

        container.appendChild(notification);

        if (timeout > 0) {
            setTimeout(() => {
                notification.style.opacity = '0';
                setTimeout(() => notification.remove(), 300);
            }, timeout);
        }

        return notification;
    },

    // ============================================================
    // WIRE PATH CALCULATION
    // Translated from: packages/node_modules/@node-red/editor-client/src/js/ui/view.js
    // Lines: 800-850 (bezier curve calculation)
    // ============================================================
    
    calculateWirePath: function(x1, y1, x2, y2) {
        const dx = Math.abs(x2 - x1);
        const dy = Math.abs(y2 - y1);
        
        // Control point distance - matches Node-RED's algorithm
        let cp = Math.max(75, dx / 2);
        
        // Adjust for backward wires
        if (x2 < x1) {
            cp = Math.max(75, Math.abs(dy) / 2);
        }
        
        return `M ${x1} ${y1} C ${x1 + cp} ${y1}, ${x2 - cp} ${y2}, ${x2} ${y2}`;
    },

    // ============================================================
    // FOCUS MANAGEMENT
    // ============================================================
    
    focusElement: function(selector) {
        const el = document.querySelector(selector);
        if (el) {
            el.focus();
        }
    },

    // ============================================================
    // LOCAL STORAGE (for persistence)
    // Translated from: packages/node_modules/@node-red/editor-client/src/js/red.js
    // ============================================================
    
    getLocalStorage: function(key) {
        try {
            return JSON.parse(localStorage.getItem('node-red-' + key));
        } catch {
            return null;
        }
    },

    setLocalStorage: function(key, value) {
        try {
            localStorage.setItem('node-red-' + key, JSON.stringify(value));
        } catch {
            // Storage not available
        }
    }
};
