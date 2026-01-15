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
        dragNodeId: null,
        dragStartX: 0,
        dragStartY: 0,
        dragStartNodeX: 0,
        dragStartNodeY: 0,
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
        offsetY: 0,
        spacebarDown: false,
        mouseDownTime: 0,
        mouseDownPos: { x: 0, y: 0 },
        dragThreshold: 5,  // pixels before drag starts
        hasMoved: false
    },

    // Initialize canvas event listeners
    initCanvas: function(canvasElement, dotNetRef) {
        if (!canvasElement) return;
        
        const self = this;
        this.canvasElement = canvasElement;
        this.dotNetRef = dotNetRef;
        
        // ============================================================
        // SOURCE: view.js - Handle spacebar for pan mode
        // ============================================================
        document.addEventListener('keydown', function(e) {
            if (e.code === 'Space' && !self.state.spacebarDown) {
                self.state.spacebarDown = true;
                canvasElement.style.cursor = 'grab';
            }
        });
        
        document.addEventListener('keyup', function(e) {
            if (e.code === 'Space') {
                self.state.spacebarDown = false;
                if (!self.state.panning) {
                    canvasElement.style.cursor = 'crosshair';
                }
            }
        });
        
        // ============================================================
        // SOURCE: view.js lines 250-280 - mousedown handling
        // ============================================================
        canvasElement.addEventListener('mousedown', function(e) {
            const rect = canvasElement.getBoundingClientRect();
            const x = (e.clientX - rect.left) / self.state.scale;
            const y = (e.clientY - rect.top) / self.state.scale;
            
            self.state.mouseDownTime = Date.now();
            self.state.mouseDownPos = { x: e.clientX, y: e.clientY };
            self.state.hasMoved = false;
            
            // Middle click OR spacebar + left click - pan mode
            if (e.button === 1 || (e.button === 0 && self.state.spacebarDown)) {
                e.preventDefault();
                self.state.panning = true;
                self.state.panStartX = e.clientX;
                self.state.panStartY = e.clientY;
                canvasElement.style.cursor = 'grabbing';
                return;
            }
            
            if (e.button === 0) { // Left click
                // Check if clicking on a port (output)
                const portElement = e.target.closest('.red-ui-flow-port');
                const nodeElement = e.target.closest('.red-ui-workspace-node');
                
                if (portElement && nodeElement && portElement.classList.contains('red-ui-flow-port-output')) {
                    // Output port - start wire drawing
                    self.state.connecting = true;
                    self.state.connectionSource = nodeElement;
                    self.state.connectionPort = portElement;
                    const nodeId = nodeElement.getAttribute('data-node-id') || '';
                    const portIndex = parseInt(portElement.getAttribute('data-port-index') || '0');
                    dotNetRef.invokeMethodAsync('OnWireDrawStart', nodeId, portIndex, x, y);
                    return;
                }
                
                if (nodeElement) {
                    // Clicked on a node - prepare for potential drag
                    self.state.dragNode = nodeElement;
                    self.state.dragNodeId = nodeElement.getAttribute('data-node-id') || '';
                    self.state.dragStartX = e.clientX;
                    self.state.dragStartY = e.clientY;
                    
                    // Get current node position from transform
                    const transform = nodeElement.getAttribute('transform') || '';
                    const match = transform.match(/translate\(([+-]?\d*\.?\d+),\s*([+-]?\d*\.?\d+)\)/);
                    if (match) {
                        self.state.dragStartNodeX = parseFloat(match[1]);
                        self.state.dragStartNodeY = parseFloat(match[2]);
                    }
                    
                    // Don't start dragging yet - wait for threshold
                    return;
                }
                
                // Clicked on empty canvas - start lasso selection
                self.state.lasso = true;
                self.state.lassoStart = { x: x, y: y };
                dotNetRef.invokeMethodAsync('OnLassoStart', x, y);
            }
        });

        // ============================================================
        // SOURCE: view.js lines 300-350 - mousemove handling
        // ============================================================
        canvasElement.addEventListener('mousemove', function(e) {
            const rect = canvasElement.getBoundingClientRect();
            const x = (e.clientX - rect.left) / self.state.scale;
            const y = (e.clientY - rect.top) / self.state.scale;
            
            // Check if we've moved past the drag threshold
            if (!self.state.hasMoved && self.state.dragNode) {
                const dx = Math.abs(e.clientX - self.state.mouseDownPos.x);
                const dy = Math.abs(e.clientY - self.state.mouseDownPos.y);
                if (dx > self.state.dragThreshold || dy > self.state.dragThreshold) {
                    self.state.hasMoved = true;
                    self.state.dragging = true;
                    dotNetRef.invokeMethodAsync('OnNodeDragStart', self.state.dragNodeId, 
                        self.state.dragStartNodeX, self.state.dragStartNodeY);
                }
            }
            
            if (self.state.dragging && self.state.dragNode) {
                const dx = (e.clientX - self.state.dragStartX) / self.state.scale;
                const dy = (e.clientY - self.state.dragStartY) / self.state.scale;
                const newX = Math.round((self.state.dragStartNodeX + dx) / 20) * 20;
                const newY = Math.round((self.state.dragStartNodeY + dy) / 20) * 20;
                dotNetRef.invokeMethodAsync('OnNodeDrag', self.state.dragNodeId, newX, newY);
            } else if (self.state.connecting) {
                dotNetRef.invokeMethodAsync('OnWireDraw', x, y);
            } else if (self.state.lasso) {
                dotNetRef.invokeMethodAsync('OnLassoDraw', 
                    self.state.lassoStart.x, self.state.lassoStart.y, x, y);
            } else if (self.state.panning) {
                const dx = e.clientX - self.state.panStartX;
                const dy = e.clientY - self.state.panStartY;
                self.state.panStartX = e.clientX;
                self.state.panStartY = e.clientY;
                dotNetRef.invokeMethodAsync('OnCanvasPan', dx, dy);
            }
        });

        // ============================================================
        // SOURCE: view.js lines 360-400 - mouseup handling
        // ============================================================
        canvasElement.addEventListener('mouseup', function(e) {
            const rect = canvasElement.getBoundingClientRect();
            const x = (e.clientX - rect.left) / self.state.scale;
            const y = (e.clientY - rect.top) / self.state.scale;
            
            if (self.state.dragging) {
                dotNetRef.invokeMethodAsync('OnNodeDragEnd', self.state.dragNodeId, x, y);
                self.state.dragging = false;
                self.state.dragNode = null;
                self.state.dragNodeId = null;
            } else if (self.state.dragNode && !self.state.hasMoved) {
                // Click without drag - select the node
                const shiftKey = e.shiftKey;
                const ctrlKey = e.ctrlKey || e.metaKey;
                dotNetRef.invokeMethodAsync('OnNodeClick', self.state.dragNodeId, shiftKey, ctrlKey);
                self.state.dragNode = null;
                self.state.dragNodeId = null;
            } else if (self.state.connecting) {
                // Check if over an input port
                const targetPort = document.elementFromPoint(e.clientX, e.clientY);
                if (targetPort?.classList.contains('red-ui-flow-port')) {
                    const targetNode = targetPort.closest('.red-ui-workspace-node');
                    if (targetNode) {
                        const targetNodeId = targetNode.getAttribute('data-node-id') || '';
                        const targetPortIndex = parseInt(targetPort.getAttribute('data-port-index') || '0');
                        dotNetRef.invokeMethodAsync('OnWireDrawEnd', targetNodeId, targetPortIndex, true);
                    } else {
                        dotNetRef.invokeMethodAsync('OnWireDrawEnd', '', 0, false);
                    }
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
                canvasElement.style.cursor = self.state.spacebarDown ? 'grab' : 'crosshair';
            } else if (!self.state.hasMoved) {
                // Click on empty canvas - deselect all
                dotNetRef.invokeMethodAsync('OnCanvasClick');
            }
            
            self.state.dragNode = null;
            self.state.hasMoved = false;
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
