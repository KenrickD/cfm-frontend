/**
 * Searchable Dropdown Component
 * Modern search selection dropdown with keyboard navigation
 *
 * Usage:
 * const dropdown = new SearchableDropdown('#mySelect', {
 *     placeholder: 'Select an option',
 *     searchPlaceholder: 'Search...',
 *     allowClear: true,
 *     data: [
 *         { value: '1', label: 'Option 1', description: 'Description 1' },
 *         { value: '2', label: 'Option 2' }
 *     ]
 * });
 */

class SearchableDropdown {
    constructor(selector, options = {}) {
        this.select = typeof selector === 'string' ? document.querySelector(selector) : selector;

        if (!this.select) {
            console.error('SearchableDropdown: Element not found', selector);
            return;
        }

        this.options = {
            placeholder: options.placeholder || 'Select an option',
            searchPlaceholder: options.searchPlaceholder || 'Search...',
            allowClear: options.allowClear !== undefined ? options.allowClear : true,
            data: options.data || null,
            ajax: options.ajax || null,
            minimumInputLength: options.minimumInputLength || 0,
            onSelect: options.onSelect || null,
            onChange: options.onChange || null,
            disabled: options.disabled || false,
            size: options.size || 'default',
            templateResult: options.templateResult || null,
            templateSelection: options.templateSelection || null,
            multiple: options.multiple || false,
            groups: options.groups || false
        };

        this.selectedValue = null;
        this.selectedLabel = null;
        this.selectedValues = [];
        this.currentIndex = -1;
        this.filteredOptions = [];

        this.init();
    }

    init() {
        this.createStructure();
        this.loadInitialData();
        this.attachEvents();

        if (this.options.disabled) {
            this.disable();
        }
    }

    createStructure() {
        this.select.style.display = 'none';

        const wrapper = document.createElement('div');
        wrapper.className = `searchable-dropdown ${this.options.size}`;
        if (this.options.disabled) wrapper.classList.add('disabled');

        const toggle = document.createElement('div');
        toggle.className = 'searchable-dropdown-toggle';
        toggle.setAttribute('tabindex', '0');
        toggle.setAttribute('role', 'combobox');
        toggle.setAttribute('aria-expanded', 'false');

        const selectedText = document.createElement('span');
        selectedText.className = 'selected-text placeholder';
        selectedText.textContent = this.options.placeholder;

        const clearIcon = document.createElement('i');
        clearIcon.className = 'ti ti-x clear-icon';

        const dropdownIcon = document.createElement('i');
        dropdownIcon.className = 'ti ti-chevron-down dropdown-icon';

        toggle.appendChild(selectedText);
        toggle.appendChild(clearIcon);
        toggle.appendChild(dropdownIcon);

        const menu = document.createElement('div');
        menu.className = 'searchable-dropdown-menu';

        const searchWrapper = document.createElement('div');
        searchWrapper.className = 'searchable-dropdown-search-wrapper';

        const searchInput = document.createElement('input');
        searchInput.type = 'text';
        searchInput.className = 'searchable-dropdown-search';
        searchInput.placeholder = this.options.searchPlaceholder;
        searchInput.setAttribute('autocomplete', 'off');

        const searchIcon = document.createElement('i');
        searchIcon.className = 'ti ti-search search-icon';

        searchWrapper.appendChild(searchInput);
        searchWrapper.appendChild(searchIcon);

        const optionsContainer = document.createElement('div');
        optionsContainer.className = 'searchable-dropdown-options';

        menu.appendChild(searchWrapper);
        menu.appendChild(optionsContainer);

        wrapper.appendChild(toggle);
        wrapper.appendChild(menu);

        this.select.parentNode.insertBefore(wrapper, this.select.nextSibling);

        this.wrapper = wrapper;
        this.toggle = toggle;
        this.selectedText = selectedText;
        this.clearIcon = clearIcon;
        this.menu = menu;
        this.searchInput = searchInput;
        this.optionsContainer = optionsContainer;
    }

    loadInitialData() {
        if (this.options.data) {
            this.renderOptions(this.options.data);
        } else {
            this.loadFromSelect();
        }
    }

    loadFromSelect() {
        const data = [];
        const options = Array.from(this.select.options);

        options.forEach(option => {
            if (option.value) {
                data.push({
                    value: option.value,
                    label: option.textContent,
                    selected: option.selected,
                    disabled: option.disabled,
                    data: option.dataset
                });
            }
        });

        this.renderOptions(data);

        const selectedOption = options.find(opt => opt.selected);
        if (selectedOption && selectedOption.value) {
            this.setValue(selectedOption.value, selectedOption.textContent, false);
        }
    }

    renderOptions(data, searchTerm = '') {
        this.filteredOptions = data.filter(item => {
            const label = item.label.toLowerCase();
            const search = searchTerm.toLowerCase();
            return label.includes(search);
        });

        this.optionsContainer.innerHTML = '';

        if (this.filteredOptions.length === 0) {
            const noResults = document.createElement('div');
            noResults.className = 'searchable-dropdown-no-results';
            if (this.options.disabled && data.length === 0) {
                noResults.innerHTML = '<i class="ti ti-info-circle me-1"></i>Please select the previous option first';
            } else {
                noResults.innerHTML = '<i class="ti ti-inbox me-1"></i>No results found';
            }
            this.optionsContainer.appendChild(noResults);
            return;
        }

        this.filteredOptions.forEach((item, index) => {
            const option = document.createElement('div');
            option.className = 'searchable-dropdown-option';
            option.setAttribute('data-value', item.value);
            option.setAttribute('data-index', index);

            if (item.disabled) {
                option.classList.add('disabled');
            }

            if (this.selectedValue === item.value) {
                option.classList.add('selected');
            }

            const optionLabel = document.createElement('span');
            optionLabel.className = 'option-label';

            if (this.options.templateResult && typeof this.options.templateResult === 'function') {
                optionLabel.innerHTML = this.options.templateResult(item);
            } else {
                optionLabel.textContent = item.label;

                if (item.description) {
                    const description = document.createElement('span');
                    description.className = 'option-description';
                    description.textContent = item.description;
                    optionLabel.appendChild(description);
                }
            }

            option.appendChild(optionLabel);
            this.optionsContainer.appendChild(option);
        });

        this.currentIndex = -1;
    }

    attachEvents() {
        this.toggle.addEventListener('click', (e) => {
            if (!this.options.disabled) {
                this.toggleMenu();
            }
        });

        this.clearIcon.addEventListener('click', (e) => {
            e.stopPropagation();
            if (!this.options.disabled) {
                this.clear();
            }
        });

        this.searchInput.addEventListener('input', (e) => {
            const searchTerm = e.target.value;

            if (this.options.ajax) {
                if (searchTerm.length >= this.options.minimumInputLength) {
                    this.loadAjaxData(searchTerm);
                } else {
                    this.optionsContainer.innerHTML = '';
                }
            } else {
                const data = this.options.data || this.loadDataFromSelect();
                this.renderOptions(data, searchTerm);
            }
        });

        this.searchInput.addEventListener('keydown', (e) => {
            this.handleKeyboard(e);
        });

        this.optionsContainer.addEventListener('click', (e) => {
            const option = e.target.closest('.searchable-dropdown-option');
            if (option && !option.classList.contains('disabled')) {
                const value = option.getAttribute('data-value');
                const label = option.querySelector('.option-label').textContent;
                this.setValue(value, label);
                this.closeMenu();
            }
        });

        document.addEventListener('click', (e) => {
            if (!this.wrapper.contains(e.target)) {
                this.closeMenu();
            }
        });

        this.toggle.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                this.toggleMenu();
            }
        });
    }

    handleKeyboard(e) {
        const options = Array.from(this.optionsContainer.querySelectorAll('.searchable-dropdown-option:not(.disabled)'));

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                this.currentIndex = Math.min(this.currentIndex + 1, options.length - 1);
                this.highlightOption(options);
                break;

            case 'ArrowUp':
                e.preventDefault();
                this.currentIndex = Math.max(this.currentIndex - 1, 0);
                this.highlightOption(options);
                break;

            case 'Enter':
                e.preventDefault();
                if (this.currentIndex >= 0 && options[this.currentIndex]) {
                    const value = options[this.currentIndex].getAttribute('data-value');
                    const label = options[this.currentIndex].querySelector('.option-label').textContent;
                    this.setValue(value, label);
                    this.closeMenu();
                }
                break;

            case 'Escape':
                e.preventDefault();
                this.closeMenu();
                this.toggle.focus();
                break;
        }
    }

    highlightOption(options) {
        options.forEach((opt, index) => {
            opt.classList.remove('active');
            if (index === this.currentIndex) {
                opt.classList.add('active');
                opt.scrollIntoView({ block: 'nearest' });
            }
        });
    }

    toggleMenu() {
        if (this.wrapper.classList.contains('active')) {
            this.closeMenu();
        } else {
            this.openMenu();
        }
    }

    openMenu() {
        this.wrapper.classList.add('active');
        this.toggle.setAttribute('aria-expanded', 'true');
        this.searchInput.focus();
        this.searchInput.value = '';

        if (this.options.data) {
            this.renderOptions(this.options.data);
        } else {
            const data = this.loadDataFromSelect();
            this.renderOptions(data);
        }
    }

    closeMenu() {
        this.wrapper.classList.remove('active');
        this.toggle.setAttribute('aria-expanded', 'false');
        this.currentIndex = -1;
    }

    setValue(value, label, triggerChange = true) {
        this.selectedValue = value;
        this.selectedLabel = label;

        this.selectedText.textContent = label;
        this.selectedText.classList.remove('placeholder');
        this.wrapper.classList.add('has-value');

        this.select.value = value;

        const event = new Event('change', { bubbles: true });
        this.select.dispatchEvent(event);

        if (triggerChange && this.options.onChange) {
            this.options.onChange(value, label);
        }

        if (this.options.onSelect) {
            this.options.onSelect(value, label);
        }
    }

    clear() {
        this.selectedValue = null;
        this.selectedLabel = null;

        this.selectedText.textContent = this.options.placeholder;
        this.selectedText.classList.add('placeholder');
        this.wrapper.classList.remove('has-value');

        this.select.value = '';

        const event = new Event('change', { bubbles: true });
        this.select.dispatchEvent(event);

        if (this.options.onChange) {
            this.options.onChange(null, null);
        }

        this.closeMenu();
    }

    getValue() {
        return this.selectedValue;
    }

    getLabel() {
        return this.selectedLabel;
    }

    loadDataFromSelect() {
        const data = [];
        const options = Array.from(this.select.options);

        options.forEach(option => {
            if (option.value) {
                data.push({
                    value: option.value,
                    label: option.textContent,
                    disabled: option.disabled,
                    data: option.dataset
                });
            }
        });

        return data;
    }

    loadAjaxData(searchTerm) {
        this.optionsContainer.innerHTML = '';
        const loading = document.createElement('div');
        loading.className = 'searchable-dropdown-loading';
        loading.innerHTML = '<i class="ti ti-loader me-1"></i>Loading...';
        this.optionsContainer.appendChild(loading);

        if (typeof this.options.ajax === 'function') {
            this.options.ajax(searchTerm)
                .then(data => {
                    this.renderOptions(data, '');
                })
                .catch(error => {
                    console.error('SearchableDropdown AJAX error:', error);
                    this.optionsContainer.innerHTML = '<div class="searchable-dropdown-no-results">Error loading data</div>';
                });
        }
    }

    disable() {
        this.options.disabled = true;
        this.wrapper.classList.add('disabled');
        this.toggle.setAttribute('tabindex', '-1');
    }

    enable() {
        this.options.disabled = false;
        this.wrapper.classList.remove('disabled');
        this.toggle.setAttribute('tabindex', '0');
    }

    destroy() {
        if (this.wrapper && this.wrapper.parentNode) {
            this.wrapper.parentNode.removeChild(this.wrapper);
        }
        this.select.style.display = '';
    }
}

// Global initialization for all dropdowns with data-searchable attribute
document.addEventListener('DOMContentLoaded', function() {
    const searchableSelects = document.querySelectorAll('select[data-searchable="true"]');
    searchableSelects.forEach(select => {
        const options = {
            placeholder: select.getAttribute('data-placeholder') || 'Select an option',
            searchPlaceholder: select.getAttribute('data-search-placeholder') || 'Search...',
            allowClear: select.getAttribute('data-allow-clear') !== 'false',
            size: select.classList.contains('form-select-sm') ? 'small' : 'default',
            disabled: select.hasAttribute('disabled') || select.disabled
        };

        new SearchableDropdown(select, options);
    });
});
