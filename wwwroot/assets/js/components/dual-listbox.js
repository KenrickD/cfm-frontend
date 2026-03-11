/**
 * Dual Listbox Component
 * Two-panel selection interface with drag & drop support
 *
 * Usage:
 * const dualListbox = new DualListbox('#container', {
 *     leftLabel: 'Available Items',
 *     rightLabel: 'Selected Items',
 *     leftItems: [{id: 1, name: 'Item 1'}],
 *     rightItems: [],
 *     searchable: true,
 *     onChange: (leftItems, rightItems) => { }
 * });
 */

(function (window) {
    'use strict';

    class DualListbox {
        constructor(container, options = {}) {
            this.container = typeof container === 'string' ? document.querySelector(container) : container;
            if (!this.container) {
                throw new Error('DualListbox: Container element not found');
            }

            this.options = {
                leftLabel: options.leftLabel || 'All Properties',
                rightLabel: options.rightLabel || 'Accessible by PIC',
                leftItems: options.leftItems || [],
                rightItems: options.rightItems || [],
                searchable: options.searchable !== false,
                idProperty: options.idProperty || 'id',
                nameProperty: options.nameProperty || 'name',
                onChange: options.onChange || null,
                disabled: options.disabled || false
            };

            this.leftItems = [...this.options.leftItems];
            this.rightItems = [...this.options.rightItems];
            this.leftSelected = [];
            this.rightSelected = [];
            this.leftSearchTerm = '';
            this.rightSearchTerm = '';

            this.init();
        }

        init() {
            this.render();
            this.attachEventListeners();
        }

        render() {
            const disabledClass = this.options.disabled ? 'disabled' : '';

            this.container.innerHTML = `
                <div class="dual-listbox-container ${disabledClass}">
                    <div class="listbox-panel">
                        <label>
                            ${this.options.leftLabel}
                            <span class="item-count">${this.leftItems.length}</span>
                        </label>
                        ${this.options.searchable ? this.renderSearch('left') : ''}
                        <div class="listbox" data-side="left" data-droppable="true">
                            <div class="listbox-items" data-side="left">
                                ${this.renderItems(this.getFilteredItems('left'), 'left')}
                            </div>
                        </div>
                    </div>
                    <div class="listbox-controls">
                        <button type="button" class="btn btn-primary" data-action="move-right" ${this.options.disabled ? 'disabled' : ''}>
                            <i class="ti ti-chevron-right"></i>
                            Choose
                        </button>
                        <button type="button" class="btn btn-secondary" data-action="move-left" ${this.options.disabled ? 'disabled' : ''}>
                            <i class="ti ti-chevron-left"></i>
                            Remove
                        </button>
                    </div>
                    <div class="listbox-panel">
                        <label>
                            ${this.options.rightLabel}
                            <span class="item-count">${this.rightItems.length}</span>
                        </label>
                        ${this.options.searchable ? this.renderSearch('right') : ''}
                        <div class="listbox" data-side="right" data-droppable="true">
                            <div class="listbox-items" data-side="right">
                                ${this.renderItems(this.getFilteredItems('right'), 'right')}
                            </div>
                        </div>
                    </div>
                </div>
            `;
        }

        renderSearch(side) {
            return `
                <div class="listbox-search">
                    <input type="text"
                           class="search-input"
                           placeholder="Search..."
                           data-side="${side}"
                           ${this.options.disabled ? 'disabled' : ''}>
                </div>
            `;
        }

        renderItems(items, side) {
            if (items.length === 0) {
                return `
                    <div class="listbox-empty">
                        <i class="ti ti-folder-off"></i>
                        <p>No items available</p>
                    </div>
                `;
            }

            return items.map(item => {
                const id = item[this.options.idProperty];
                const name = item[this.options.nameProperty];
                const isSelected = side === 'left'
                    ? this.leftSelected.includes(id)
                    : this.rightSelected.includes(id);

                return `
                    <div class="listbox-item ${isSelected ? 'selected' : ''}"
                         data-id="${id}"
                         data-side="${side}"
                         draggable="${!this.options.disabled}">
                        ${this.escapeHtml(name)}
                    </div>
                `;
            }).join('');
        }

        getFilteredItems(side) {
            const items = side === 'left' ? this.leftItems : this.rightItems;
            const searchTerm = side === 'left' ? this.leftSearchTerm : this.rightSearchTerm;

            if (!searchTerm) return items;

            return items.filter(item => {
                const name = item[this.options.nameProperty].toLowerCase();
                return name.includes(searchTerm.toLowerCase());
            });
        }

        attachEventListeners() {
            const container = this.container.querySelector('.dual-listbox-container');

            // Search inputs
            if (this.options.searchable) {
                container.querySelectorAll('.search-input').forEach(input => {
                    input.addEventListener('input', (e) => {
                        const side = e.target.dataset.side;
                        if (side === 'left') {
                            this.leftSearchTerm = e.target.value;
                        } else {
                            this.rightSearchTerm = e.target.value;
                        }
                        this.updateItemsDisplay(side);
                    });
                });
            }

            // Item selection
            container.addEventListener('click', (e) => {
                const item = e.target.closest('.listbox-item');
                if (item) {
                    const id = parseInt(item.dataset.id);
                    const side = item.dataset.side;
                    this.toggleSelection(id, side, e.ctrlKey || e.metaKey);
                }
            });

            // Move buttons
            container.querySelector('[data-action="move-right"]')?.addEventListener('click', () => {
                this.moveItems('right');
            });

            container.querySelector('[data-action="move-left"]')?.addEventListener('click', () => {
                this.moveItems('left');
            });

            // Drag and drop
            this.attachDragDropListeners(container);
        }

        attachDragDropListeners(container) {
            if (this.options.disabled) return;

            // Drag start
            container.addEventListener('dragstart', (e) => {
                const item = e.target.closest('.listbox-item');
                if (item) {
                    const id = parseInt(item.dataset.id);
                    const side = item.dataset.side;

                    e.dataTransfer.effectAllowed = 'move';
                    e.dataTransfer.setData('text/plain', JSON.stringify({ id, side }));
                    item.classList.add('dragging');
                }
            });

            // Drag end
            container.addEventListener('dragend', (e) => {
                const item = e.target.closest('.listbox-item');
                if (item) {
                    item.classList.remove('dragging');
                }

                container.querySelectorAll('.drag-over-zone').forEach(el => {
                    el.classList.remove('drag-over-zone');
                });
            });

            // Drag over
            container.addEventListener('dragover', (e) => {
                e.preventDefault();
                const dropZone = e.target.closest('.listbox[data-droppable="true"]');
                if (dropZone) {
                    e.dataTransfer.dropEffect = 'move';
                    dropZone.classList.add('drag-over-zone');
                }
            });

            // Drag leave
            container.addEventListener('dragleave', (e) => {
                const dropZone = e.target.closest('.listbox[data-droppable="true"]');
                if (dropZone && !dropZone.contains(e.relatedTarget)) {
                    dropZone.classList.remove('drag-over-zone');
                }
            });

            // Drop
            container.addEventListener('drop', (e) => {
                e.preventDefault();
                const dropZone = e.target.closest('.listbox[data-droppable="true"]');
                if (dropZone) {
                    dropZone.classList.remove('drag-over-zone');

                    const data = JSON.parse(e.dataTransfer.getData('text/plain'));
                    const targetSide = dropZone.dataset.side;

                    if (data.side !== targetSide) {
                        this.moveItemById(data.id, data.side, targetSide);
                    }
                }
            });
        }

        toggleSelection(id, side, multiSelect) {
            const selectedArray = side === 'left' ? this.leftSelected : this.rightSelected;
            const index = selectedArray.indexOf(id);

            if (!multiSelect) {
                if (side === 'left') {
                    this.leftSelected = index >= 0 ? [] : [id];
                } else {
                    this.rightSelected = index >= 0 ? [] : [id];
                }
            } else {
                if (index >= 0) {
                    selectedArray.splice(index, 1);
                } else {
                    selectedArray.push(id);
                }
            }

            this.updateItemsDisplay(side);
        }

        moveItems(direction) {
            if (direction === 'right') {
                const itemsToMove = this.leftItems.filter(item =>
                    this.leftSelected.includes(item[this.options.idProperty])
                );

                this.leftItems = this.leftItems.filter(item =>
                    !this.leftSelected.includes(item[this.options.idProperty])
                );

                this.rightItems.push(...itemsToMove);
                this.leftSelected = [];
            } else {
                const itemsToMove = this.rightItems.filter(item =>
                    this.rightSelected.includes(item[this.options.idProperty])
                );

                this.rightItems = this.rightItems.filter(item =>
                    !this.rightSelected.includes(item[this.options.idProperty])
                );

                this.leftItems.push(...itemsToMove);
                this.rightSelected = [];
            }

            this.updateDisplay();
            this.triggerChange();
        }

        moveItemById(id, fromSide, toSide) {
            if (fromSide === toSide) return;

            const fromItems = fromSide === 'left' ? this.leftItems : this.rightItems;
            const toItems = toSide === 'left' ? this.leftItems : this.rightItems;

            const itemIndex = fromItems.findIndex(item => item[this.options.idProperty] === id);
            if (itemIndex >= 0) {
                const [item] = fromItems.splice(itemIndex, 1);
                toItems.push(item);

                this.updateDisplay();
                this.triggerChange();
            }
        }

        updateDisplay() {
            this.updateItemsDisplay('left');
            this.updateItemsDisplay('right');
            this.updateCounts();
        }

        updateItemsDisplay(side) {
            const container = this.container.querySelector(`.listbox-items[data-side="${side}"]`);
            if (container) {
                container.innerHTML = this.renderItems(this.getFilteredItems(side), side);
            }
        }

        updateCounts() {
            const leftCount = this.container.querySelector('.listbox-panel:first-child .item-count');
            const rightCount = this.container.querySelector('.listbox-panel:last-child .item-count');

            if (leftCount) leftCount.textContent = this.leftItems.length;
            if (rightCount) rightCount.textContent = this.rightItems.length;
        }

        triggerChange() {
            if (typeof this.options.onChange === 'function') {
                this.options.onChange(this.leftItems, this.rightItems);
            }
        }

        getRightItems() {
            return [...this.rightItems];
        }

        getRightItemIds() {
            return this.rightItems.map(item => item[this.options.idProperty]);
        }

        setLeftItems(items) {
            this.leftItems = [...items];
            this.leftSelected = [];
            this.leftSearchTerm = '';
            this.updateDisplay();
        }

        setRightItems(items) {
            this.rightItems = [...items];
            this.rightSelected = [];
            this.rightSearchTerm = '';
            this.updateDisplay();
        }

        setDisabled(disabled) {
            this.options.disabled = disabled;
            this.render();
            this.attachEventListeners();
        }

        clear() {
            this.leftItems = [];
            this.rightItems = [];
            this.leftSelected = [];
            this.rightSelected = [];
            this.leftSearchTerm = '';
            this.rightSearchTerm = '';
            this.updateDisplay();
        }

        escapeHtml(text) {
            if (!text) return '';
            const map = {
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                '"': '&quot;',
                "'": '&#039;'
            };
            return String(text).replace(/[&<>"']/g, m => map[m]);
        }
    }

    window.DualListbox = DualListbox;

})(window);
