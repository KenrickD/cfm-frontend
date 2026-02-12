// Priority Level List Page JavaScript
// Data is now server-rendered, this file handles search, move, and delete actions
(function () {
    'use strict';

    // Client context for multi-tab session safety
    const clientContext = {
        get idClient() { return window.PageContext?.idClient || 0; }
    };

    let deleteModal;
    let currentDeleteId = null;
    let currentDeleteName = '';

    // Initialize on DOM load
    document.addEventListener('DOMContentLoaded', function () {
        initializeComponents();
    });

    function initializeComponents() {
        // Initialize delete modal
        const modalElement = document.getElementById('deleteConfirmModal');
        if (modalElement) {
            deleteModal = new bootstrap.Modal(modalElement);
        }

        // Search handling
        const searchBtn = document.getElementById('searchBtn');
        if (searchBtn) {
            searchBtn.addEventListener('click', handleSearch);
        }

        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.addEventListener('keypress', function (e) {
                if (e.which === 13) {
                    e.preventDefault();
                    handleSearch();
                }
            });
        }

        const clearSearchBtn = document.getElementById('clearSearchBtn');
        if (clearSearchBtn) {
            clearSearchBtn.addEventListener('click', clearSearch);
        }

        // Confirm delete button
        const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
        if (confirmDeleteBtn) {
            confirmDeleteBtn.addEventListener('click', handleDelete);
        }
    }

    function handleSearch() {
        const searchValue = document.getElementById('searchInput')?.value?.trim() || '';
        const url = new URL(window.location.href);

        if (searchValue) {
            url.searchParams.set('search', searchValue);
        } else {
            url.searchParams.delete('search');
        }
        url.searchParams.set('page', '1');

        window.location.href = url.toString();
    }

    function clearSearch() {
        const url = new URL(window.location.href);
        url.searchParams.delete('search');
        url.searchParams.set('page', '1');
        window.location.href = url.toString();
    }

    // View priority level detail
    window.viewPriorityLevel = function (id) {
        window.location.href = `/Helpdesk/PriorityLevelDetail?id=${id}`;
    };

    // Edit priority level
    window.editPriorityLevel = function (id) {
        window.location.href = `/Helpdesk/PriorityLevelEdit?id=${id}`;
    };

    // Show delete modal
    window.showDeleteModal = function (id, name) {
        currentDeleteId = id;
        currentDeleteName = name;
        const deleteNameElement = document.getElementById('deletePriorityName');
        if (deleteNameElement) {
            deleteNameElement.textContent = name;
        }
        if (deleteModal) {
            deleteModal.show();
        }
    };

    // Move priority level up
    window.movePriorityLevelUp = async function (id) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        try {
            const response = await fetch(`${MvcEndpoints.Helpdesk.Settings.PriorityLevel.MoveUp}?id=${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                }
            });

            const result = await response.json();

            if (result.success) {
                window.location.reload();
            } else {
                showNotification(result.message || 'Failed to move priority level up', 'error');
            }
        } catch (error) {
            console.error('Error moving priority level up:', error);
            showNotification('An error occurred while moving the priority level', 'error');
        }
    };

    // Move priority level down
    window.movePriorityLevelDown = async function (id) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        try {
            const response = await fetch(`${MvcEndpoints.Helpdesk.Settings.PriorityLevel.MoveDown}?id=${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                }
            });

            const result = await response.json();

            if (result.success) {
                window.location.reload();
            } else {
                showNotification(result.message || 'Failed to move priority level down', 'error');
            }
        } catch (error) {
            console.error('Error moving priority level down:', error);
            showNotification('An error occurred while moving the priority level', 'error');
        }
    };

    // Handle delete confirmation
    async function handleDelete() {
        if (!currentDeleteId) return;

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const confirmBtn = document.getElementById('confirmDeleteBtn');

        // Show loading state
        if (confirmBtn) {
            confirmBtn.disabled = true;
            confirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Deleting...';
        }

        try {
            const response = await fetch(`${MvcEndpoints.Helpdesk.Settings.PriorityLevel.Delete}?id=${currentDeleteId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                }
            });

            const result = await response.json();

            if (result.success) {
                showNotification('Priority level deleted successfully', 'success');
                if (deleteModal) {
                    deleteModal.hide();
                }
                // Reload the page to refresh the list
                window.location.reload();
            } else {
                showNotification(result.message || 'Failed to delete priority level', 'error');
            }
        } catch (error) {
            console.error('Error deleting priority level:', error);
            showNotification('An error occurred while deleting the priority level', 'error');
        } finally {
            // Reset button state
            if (confirmBtn) {
                confirmBtn.disabled = false;
                confirmBtn.innerHTML = 'Delete';
            }
            currentDeleteId = null;
            currentDeleteName = '';
        }
    }
})();
