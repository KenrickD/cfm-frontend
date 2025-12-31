/**
 * Job Code List Page JavaScript
 * Handles search, filtering, and CRUD operations for job codes
 */

(function() {
    'use strict';

    let deleteJobCodeId = null;
    let deleteJobCodeName = '';

    // Initialize tooltips
    function initTooltips() {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // Get current query parameters
    function getQueryParams() {
        const params = new URLSearchParams(window.location.search);
        return {
            search: params.get('search') || '',
            group: params.get('group') || '',
            showDeleted: params.get('showDeleted') === 'true',
            page: params.get('page') || '1'
        };
    }

    // Build URL with query parameters
    function buildUrl(params) {
        const url = new URL(window.location.href);
        url.search = '';

        Object.keys(params).forEach(key => {
            if (params[key] !== '' && params[key] !== null && params[key] !== undefined) {
                url.searchParams.set(key, params[key]);
            }
        });

        return url.toString();
    }

    // Search functionality
    function handleSearch() {
        const searchValue = document.getElementById('mainSearchInput').value.trim();
        const currentParams = getQueryParams();

        const newParams = {
            search: searchValue,
            group: currentParams.group,
            showDeleted: currentParams.showDeleted,
            page: 1
        };

        window.location.href = buildUrl(newParams);
    }

    // Clear search
    function handleClearSearch() {
        window.location.href = window.location.pathname;
    }

    // Toggle filter section
    function handleFilterToggle() {
        const filterSection = document.getElementById('filterSection');
        const bsCollapse = new bootstrap.Collapse(filterSection, {
            toggle: true
        });
    }

    // Apply filters
    function handleApplyFilters() {
        const currentParams = getQueryParams();
        const groupValue = document.getElementById('groupFilter').value;
        const showDeletedValue = document.getElementById('showDeletedCheckbox').checked;

        const newParams = {
            search: currentParams.search,
            group: groupValue,
            showDeleted: showDeletedValue,
            page: 1
        };

        window.location.href = buildUrl(newParams);
    }

    // Clear filters
    function handleClearFilters() {
        const currentParams = getQueryParams();

        const newParams = {
            search: currentParams.search,
            page: 1
        };

        window.location.href = buildUrl(newParams);
    }

    // Handle edit job code
    function handleEditJobCode(id) {
        window.location.href = `/JobCode/Edit/${id}`;
    }

    // Handle delete job code
    function handleDeleteJobCode(id, name) {
        deleteJobCodeId = id;
        deleteJobCodeName = name;

        document.getElementById('jobCodeNameToDelete').textContent = name;

        const modal = new bootstrap.Modal(document.getElementById('deleteConfirmModal'));
        modal.show();
    }

    // Confirm delete job code
    function confirmDeleteJobCode() {
        if (!deleteJobCodeId) return;

        const confirmBtn = document.getElementById('confirmDeleteBtn');
        const originalText = confirmBtn.innerHTML;
        confirmBtn.disabled = true;
        confirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status"></span>Deleting...';

        fetch(`/JobCode/Delete/${deleteJobCodeId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            }
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showNotification(data.message || 'Job code deleted successfully', 'success', 'Success');

                const modal = bootstrap.Modal.getInstance(document.getElementById('deleteConfirmModal'));
                modal.hide();

                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            } else {
                showNotification(data.message || 'Failed to delete job code', 'error', 'Error');
                confirmBtn.disabled = false;
                confirmBtn.innerHTML = originalText;
            }
        })
        .catch(error => {
            console.error('Error deleting job code:', error);
            showNotification('Network error. Please try again.', 'error', 'Error');
            confirmBtn.disabled = false;
            confirmBtn.innerHTML = originalText;
        });
    }

    // Handle add job code
    function handleAddJobCode() {
        window.location.href = '/JobCode/Add';
    }

    // Initialize event listeners
    function initEventListeners() {
        // Search button
        const searchButton = document.getElementById('searchButton');
        if (searchButton) {
            searchButton.addEventListener('click', handleSearch);
        }

        // Search input - Enter key
        const searchInput = document.getElementById('mainSearchInput');
        if (searchInput) {
            searchInput.addEventListener('keypress', function(e) {
                if (e.key === 'Enter') {
                    handleSearch();
                }
            });
        }

        // Clear search button
        const clearSearchButton = document.getElementById('clearSearchButton');
        if (clearSearchButton) {
            clearSearchButton.addEventListener('click', handleClearSearch);
        }

        // Filter toggle button
        const filterToggleButton = document.getElementById('filterToggleButton');
        if (filterToggleButton) {
            filterToggleButton.addEventListener('click', handleFilterToggle);
        }

        // Apply filters button
        const applyFiltersButton = document.getElementById('applyFiltersButton');
        if (applyFiltersButton) {
            applyFiltersButton.addEventListener('click', handleApplyFilters);
        }

        // Clear filters button
        const clearFiltersButton = document.getElementById('clearFiltersButton');
        if (clearFiltersButton) {
            clearFiltersButton.addEventListener('click', handleClearFilters);
        }

        // Add job code button
        const addJobCodeButton = document.getElementById('addJobCodeButton');
        if (addJobCodeButton) {
            addJobCodeButton.addEventListener('click', handleAddJobCode);
        }

        const addJobCodeButtonEmpty = document.getElementById('addJobCodeButtonEmpty');
        if (addJobCodeButtonEmpty) {
            addJobCodeButtonEmpty.addEventListener('click', handleAddJobCode);
        }

        // Edit buttons
        const editButtons = document.querySelectorAll('.edit-job-code');
        editButtons.forEach(button => {
            button.addEventListener('click', function() {
                const id = this.getAttribute('data-id');
                handleEditJobCode(id);
            });
        });

        // Delete buttons
        const deleteButtons = document.querySelectorAll('.delete-job-code');
        deleteButtons.forEach(button => {
            button.addEventListener('click', function() {
                const id = this.getAttribute('data-id');
                const name = this.getAttribute('data-name');
                handleDeleteJobCode(id, name);
            });
        });

        // Confirm delete button
        const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
        if (confirmDeleteBtn) {
            confirmDeleteBtn.addEventListener('click', confirmDeleteJobCode);
        }

        // Reset delete modal on close
        const deleteModal = document.getElementById('deleteConfirmModal');
        if (deleteModal) {
            deleteModal.addEventListener('hidden.bs.modal', function() {
                deleteJobCodeId = null;
                deleteJobCodeName = '';
                const confirmBtn = document.getElementById('confirmDeleteBtn');
                confirmBtn.disabled = false;
                confirmBtn.innerHTML = '<i class="ti ti-trash me-2"></i>Delete Job Code';
            });
        }
    }

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function() {
        initTooltips();
        initEventListeners();

        // Auto-open filter section if filters are applied
        const currentParams = getQueryParams();
        if (currentParams.group || currentParams.showDeleted) {
            const filterSection = document.getElementById('filterSection');
            if (filterSection) {
                filterSection.classList.add('show');
            }
        }
    });

})();
